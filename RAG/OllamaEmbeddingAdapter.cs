using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.Config;
using UnityEngine;
using UnityEngine.Networking;

namespace NPC_AI.RAG
{
    /// Generates dense vector embeddings via Ollama's /api/embed endpoint.
    /// Recommended model: nomic-embed-text (pull with: ollama pull nomic-embed-text)
    ///
    /// Configure the model name in LLMConfig.OllamaEmbeddingModelName.
    /// This adapter is separate from OllamaAdapter — load it independently.
    public class OllamaEmbeddingAdapter : IEmbeddingService
    {
        private readonly string _baseUrl;
        private readonly string _modelName;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private bool _ready;
        private bool _disposed;

        public int Dimensions { get; private set; }
        public bool IsReady => _ready && !_disposed;

        public OllamaEmbeddingAdapter(LLMConfig config)
        {
            _baseUrl = config.OllamaBaseUrl.TrimEnd('/');
            _modelName = config.OllamaEmbeddingModelName;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                var probe = await EmbedInternalAsync("probe", ct);
                Dimensions = probe.Length;
                _ready = true;
                Debug.Log($"[OllamaEmbeddingAdapter] Ready. Model={_modelName} Dims={Dimensions}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OllamaEmbeddingAdapter] Init failed: {ex.Message}");
            }
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            if (!IsReady) throw new InvalidOperationException("OllamaEmbeddingAdapter not initialized.");

            await _lock.WaitAsync(ct);
            try
            {
                return await EmbedInternalAsync(text, ct);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<float[]> EmbedInternalAsync(string text, CancellationToken ct)
        {
            var body = $"{{\"model\":{EscapeJson(_modelName)},\"input\":{EscapeJson(text)}}}";
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var url = $"{_baseUrl}/api/embed";

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"HTTP {req.responseCode}: {req.error}");

            return ParseEmbeddingResponse(req.downloadHandler.text);
        }

        // Parses {"embeddings":[[0.1,0.2,...]]} (Ollama 0.5+) or
        //        {"embedding":[0.1,0.2,...]}     (older Ollama format)
        private static float[] ParseEmbeddingResponse(string json)
        {
            int start, end;

            const string newMarker = "\"embeddings\":[[";
            int newPos = json.IndexOf(newMarker, StringComparison.Ordinal);
            if (newPos >= 0)
            {
                start = newPos + newMarker.Length;
                end = json.IndexOf(']', start);
            }
            else
            {
                const string oldMarker = "\"embedding\":[";
                int oldPos = json.IndexOf(oldMarker, StringComparison.Ordinal);
                if (oldPos < 0) throw new Exception($"No embedding array in response: {json.Substring(0, Mathf.Min(200, json.Length))}");
                start = oldPos + oldMarker.Length;
                end = json.IndexOf(']', start);
            }

            if (end < 0) throw new Exception("Malformed embedding response: no closing bracket.");

            var segment = json.Substring(start, end - start);
            var parts = segment.Split(',');
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = float.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);

            return result;
        }

        private static string EscapeJson(string s) =>
            "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r") + "\"";

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lock.Dispose();
        }
    }
}
