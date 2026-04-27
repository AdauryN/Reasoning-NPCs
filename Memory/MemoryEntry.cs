using System;

namespace NPC_AI.Memory
{
    public enum MemoryType
    {
        Decision,
        Observation,
        PlayerAction,
        Summary
    }

    [Serializable]
    public class MemoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public MemoryType Type { get; set; } = MemoryType.Decision;

        /// Human-readable text injected into the prompt.
        public string Content { get; set; }

        /// 0–1 importance score used for retrieval ranking.
        public float Importance { get; set; } = 0.5f;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// Topic tags for lightweight filtering 
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// Embedding vector —  Null until RAG implemented.
        public float[] Embedding { get; set; }
    }
}
