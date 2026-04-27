using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NPC_AI.LLM;
using UnityEngine;

namespace NPC_AI.Memory
{
    /// Periodically compresses old memory entries by asking the LLM to summarize them.
    /// Prevents unbounded memory growth without losing important context.
    /// Runs every consolidationIntervalSeconds of play time.
    public class MemoryConsolidator
    {
        private const float ConsolidationIntervalSeconds = 300f; // every 5 minutes
        private const int BatchSize = 10;    // summarize this many entries at once
        private const int RetainCount = 20;  // keep the N most recent entries raw

        private readonly ILLMService _llm;
        private float _timer;

        public MemoryConsolidator(ILLMService llm)
        {
            _llm = llm;
        }

        public void Tick(float deltaTime)
        {
            _timer += deltaTime;
        }

        public bool NeedsConsolidation => _timer >= ConsolidationIntervalSeconds;

        public async Task<List<MemoryEntry>> ConsolidateAsync(
            List<MemoryEntry> entries, CancellationToken ct = default)
        {
            if (entries.Count <= RetainCount)
            {
                _timer = 0f;
                return entries;
            }

            var toSummarize = entries
                .OrderBy(e => e.Timestamp)
                .Take(entries.Count - RetainCount)
                .ToList();

            var batches = SplitIntoBatches(toSummarize, BatchSize);
            var summaries = new List<MemoryEntry>();

            foreach (var batch in batches)
            {
                var summary = await SummarizeBatchAsync(batch, ct);
                if (summary != null) summaries.Add(summary);
            }

            var retained = entries
                .OrderByDescending(e => e.Timestamp)
                .Take(RetainCount)
                .ToList();

            _timer = 0f;
            return summaries.Concat(retained).ToList();
        }

        private async Task<MemoryEntry> SummarizeBatchAsync(
            List<MemoryEntry> batch, CancellationToken ct)
        {
            var content = string.Join("\n", batch.Select(e => $"- {e.Content}"));
            var request = new LLMRequest
            {
                SystemPrompt = "You are summarizing NPC memories. Be concise.",
                UserMessage = $"Summarize these events into one sentence:\n{content}",
                MaxTokens = 80,
                Temperature = 0.3f
            };

            try
            {
                var response = await _llm.CompleteAsync(request, ct);
                if (!response.Success) return null;

                return new MemoryEntry
                {
                    Type = MemoryType.Summary,
                    Content = response.Text.Trim(),
                    Importance = batch.Max(e => e.Importance),
                    Timestamp = batch.Max(e => e.Timestamp),
                    Tags = new[] { "summary" }
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MemoryConsolidator] Summarization failed: {ex.Message}");
                return null;
            }
        }

        private static List<List<T>> SplitIntoBatches<T>(List<T> items, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < items.Count; i += batchSize)
                batches.Add(items.GetRange(i, Math.Min(batchSize, items.Count - i)));
            return batches;
        }
    }
}
