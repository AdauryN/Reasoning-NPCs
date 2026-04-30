using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NPC_AI.RAG
{
    /// <summary>
    /// ScriptableObject holding game lore and world knowledge for NPC RAG retrieval.
    /// Embeddings are baked at edit-time via BakeEmbeddingsAsync() (call from a custom Editor tool).
    /// At runtime, Query() returns the most relevant chunks for a given context string.
    ///
    /// Create via Assets > Create > NPC AI > Game Knowledge Base.
    /// </summary>
    [CreateAssetMenu(fileName = "GameKnowledgeBase", menuName = "NPC AI/Game Knowledge Base")]
    public class GameKnowledgeBase : ScriptableObject
    {
        [SerializeField] private List<KnowledgeChunk> chunks = new List<KnowledgeChunk>();

        private VectorIndex<KnowledgeChunk> _index;
        private bool _indexBuilt;

        public IReadOnlyList<KnowledgeChunk> Chunks => chunks;

        /// <summary>
        /// Call once at runtime (e.g., in NPCController.Start()) to build the in-memory vector index
        /// from the pre-baked embeddings stored in the ScriptableObject.
        /// </summary>
        public void BuildIndex()
        {
            _index = new VectorIndex<KnowledgeChunk>();
            foreach (var chunk in chunks)
            {
                if (chunk.Embedding != null && chunk.Embedding.Length > 0)
                    _index.Add(chunk.Embedding, chunk);
            }
            _indexBuilt = true;
            Debug.Log($"[GameKnowledgeBase] Index built with {_index.Count} chunks.");
        }

        /// <summary>
        /// Returns the top-K most relevant knowledge chunks for a query embedding.
        /// Requires BuildIndex() to have been called first.
        /// </summary>
        public List<KnowledgeChunk> Query(float[] queryEmbedding, int topK = 2)
        {
            if (!_indexBuilt)
            {
                Debug.LogWarning("[GameKnowledgeBase] Index not built. Call BuildIndex() first.");
                return new List<KnowledgeChunk>();
            }
            return _index.Search(queryEmbedding, topK);
        }

        /// <summary>
        /// Editor-time utility: embed all chunks and store the vectors in the ScriptableObject.
        /// Call from an Editor menu item or custom Inspector button — not at runtime.
        /// </summary>
        public async Task BakeEmbeddingsAsync(IEmbeddingService embedder, CancellationToken ct = default)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                if (ct.IsCancellationRequested) break;
                chunks[i].Embedding = await embedder.EmbedAsync(chunks[i].Content, ct);
                Debug.Log($"[GameKnowledgeBase] Baked {i + 1}/{chunks.Count}: {chunks[i].Id}");
            }
        }

        public void AddChunk(KnowledgeChunk chunk)
        {
            chunks.Add(chunk);
            _indexBuilt = false; // rebuild required
        }
    }
}
