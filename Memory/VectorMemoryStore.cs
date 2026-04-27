using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.Core;
using NPC_AI.RAG;
using UnityEngine;

namespace NPC_AI.Memory
{
    /// <summary>
    /// Phase 4b memory store: extends InMemoryStore with semantic vector search.
    /// Falls back to recency scoring for entries that have no embedding yet.
    /// </summary>
    public class VectorMemoryStore : INPCMemoryStore
    {
        private readonly IEmbeddingService _embedder;
        private readonly List<MemoryEntry> _entries = new List<MemoryEntry>();
        private readonly VectorIndex<MemoryEntry> _index = new VectorIndex<MemoryEntry>();
        private const int MaxEntries = 200;

        public VectorMemoryStore(IEmbeddingService embedder)
        {
            _embedder = embedder;
        }

        public async Task AddAsync(MemoryEntry entry)
        {
            try
            {
                entry.Embedding = await _embedder.EmbedAsync(entry.Content);
                _index.Add(entry.Embedding, entry);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VectorMemoryStore] Embedding failed, storing without vector: {ex.Message}");
            }

            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
            {
                var oldest = _entries[0];
                _entries.RemoveAt(0);
                _index.Remove(oldest);
            }
        }

        public List<MemoryEntry> GetRelevant(NPCWorldView context, int topK)
        {
            // Strategy:
            // 1. Always include the 3 most recent entries (recency guarantee)
            // 2. Fill remaining slots with vector-similar entries
            var recent = _entries.TakeLast(3).ToList();
            int remaining = topK - recent.Count;

            if (remaining <= 0 || _index.Count == 0) return recent;

            // Build a query string from the world view to get a useful embedding.
            var queryText = $"{context.LastPlayerAction} {context.NpcHealthPct:F1} hp distance {context.DistanceToPlayer:F0}";

            List<MemoryEntry> semantic;
            try
            {
                // Synchronous call here because GetRelevant is called from synchronous context in NPCBrain.
                // The embedding model is fast enough (~5ms) that blocking is acceptable.
                var embedding = _embedder.EmbedAsync(queryText).GetAwaiter().GetResult();
                semantic = _index.Search(embedding, remaining + 3)
                    .Where(e => !recent.Contains(e))
                    .Take(remaining)
                    .ToList();
            }
            catch
            {
                semantic = new List<MemoryEntry>();
            }

            return recent.Concat(semantic).ToList();
        }

        public Task ClearAsync()
        {
            _entries.Clear();
            _index.Clear();
            return Task.CompletedTask;
        }
    }
}
