using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPC_AI.Core;

namespace NPC_AI.Memory
{
    public class InMemoryStore : INPCMemoryStore
    {
        private const float RecencyWeight   = 0.6f;
        private const float ImportanceWeight = 0.4f;
        private const int   MaxEntries       = 200;

        private readonly List<MemoryEntry> _entries = new List<MemoryEntry>();

        public Task AddAsync(MemoryEntry entry)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0); // drop oldest
            return Task.CompletedTask;
        }

        public List<MemoryEntry> GetRelevant(NPCWorldView context, int topK)
        {
            if (_entries.Count == 0) return new List<MemoryEntry>();

            var now = DateTime.UtcNow;
            double maxAgeSeconds = (_entries.Count > 0)
                ? (now - _entries[0].Timestamp).TotalSeconds
                : 1.0;
            if (maxAgeSeconds < 1.0) maxAgeSeconds = 1.0;

            return _entries
                .Select(e =>
                {
                    double ageSeconds = (now - e.Timestamp).TotalSeconds;
                    float recencyScore = 1f - (float)(ageSeconds / maxAgeSeconds);
                    float score = RecencyWeight * recencyScore + ImportanceWeight * e.Importance;
                    return (score, e);
                })
                .OrderByDescending(x => x.score)
                .Take(topK)
                .Select(x => x.e)
                .ToList();
        }

        public Task ClearAsync()
        {
            _entries.Clear();
            return Task.CompletedTask;
        }
    }
}
