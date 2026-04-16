using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace NPC_AI.LLM
{
    /// Wraps an ILLMService and serializes all inference calls through an async queue.
    /// This is required because llama.cpp contexts are not thread-safe: multiple NPCs
    /// calling CompleteAsync concurrently would cause undefined behavior. Each NPC
    /// awaits a Task that resolves when its turn arrives in the queue.
    public class LLMRequestQueue : ILLMService
    {
        private readonly ILLMService _inner;
        private readonly ConcurrentQueue<QueuedRequest> _queue = new ConcurrentQueue<QueuedRequest>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _workerCts = new CancellationTokenSource();
        private bool _disposed;

        public bool IsReady => _inner.IsReady;

        public LLMRequestQueue(ILLMService inner)
        {
            _inner = inner;
           
            _ = ProcessQueueAsync(_workerCts.Token);
        }

        public Task InitializeAsync(CancellationToken ct = default) =>
            _inner.InitializeAsync(ct);

        public Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<LLMResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            ct.Register(() => tcs.TrySetCanceled(ct));

            _queue.Enqueue(new QueuedRequest()
            {
                Request = request,
                Completion = tcs,
                CancellationToken = ct
            });
            _signal.Release(); //release the worker
            
            ;

            return tcs.Task;
        }

        public async Task StreamAsync(LLMRequest request, Action<string> onToken, CancellationToken ct = default)
        {
            var response = await CompleteAsync(request, ct);
            if (response.Success)
                onToken?.Invoke(response.Text);
        }

        private async Task ProcessQueueAsync(CancellationToken ct)
        {
           while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                if(! _queue.TryDequeue(out var item))
                    continue;
                
                if (item.CancellationToken.IsCancellationRequested)
                {
                    item.Completion.TrySetCanceled(item.CancellationToken);
                    continue;
                }

                try
                {
                    var response = await _inner.CompleteAsync(item.Request, item.CancellationToken);
                    item.Completion.TrySetResult(response);
                }
                catch (OperationCanceledException oce)
                {
                    item.Completion.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LLMRequestQueue] Unhandled error: {ex.Message}");
                    item.Completion.TrySetResult(LLMResponse.Failure(ex.Message));
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _workerCts.Cancel();
            _workerCts.Dispose();
            _signal.Dispose();
            _inner.Dispose();
        }

        private class QueuedRequest
        {
            public LLMRequest Request;
            public TaskCompletionSource<LLMResponse> Completion;
            public CancellationToken CancellationToken;
        }
    }
}
