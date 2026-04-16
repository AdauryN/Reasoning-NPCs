using System;
using System.Threading;
using System.Threading.Tasks;

namespace NPC_AI.LLM
{
    /// <summary>
    /// Central abstraction for all LLM backends. All NPC systems depend on this interface,
    /// never on a concrete adapter.
    /// </summary>
    public interface ILLMService : IDisposable
    {
        bool IsReady { get; }

        Task InitializeAsync(CancellationToken ct = default);

        Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken ct = default);

        /// <summary>
        /// Streaming variant — fires <paramref name="onToken"/> for each token as it arrives.
        /// </summary>
        Task StreamAsync(LLMRequest request, Action<string> onToken, CancellationToken ct = default);
    }
}
