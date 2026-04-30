using System;
using System.Threading;
using System.Threading.Tasks;

namespace NPC_AI.RAG
{
    public interface IEmbeddingService : IDisposable
    {
        int Dimensions { get; }
        bool IsReady { get; }
        Task InitializeAsync(CancellationToken ct = default);
        Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    }
}
