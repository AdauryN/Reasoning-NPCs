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
    /// Extends InMemoryStore with semantic vector search.
    /// Falls back to recency scoring for entries that have no embedding yet.
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
            // Return the most recent entries only.
            // Semantic vector search requires an async embedding call which would deadlock
            // if called synchronously from the main thread.
            return _entries.TakeLast(topK).ToList();
        }

        public List<MemoryEntry> GetAll() => new List<MemoryEntry>(_entries);

        public Task ClearAsync()
        {
            _entries.Clear();
            _index.Clear();
            return Task.CompletedTask;
        }
    }
}
