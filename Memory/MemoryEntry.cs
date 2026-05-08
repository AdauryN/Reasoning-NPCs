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
        // Public fields — required for JsonUtility serialization.
        public string Id = Guid.NewGuid().ToString("N");
        public MemoryType Type = MemoryType.Decision;
        public string Content;
        public float Importance = 0.5f;
        public long TimestampTicks = DateTime.UtcNow.Ticks;
        public string[] Tags = Array.Empty<string>();

        // Embedding is re-generated on load by VectorMemoryStore — not persisted.
        [NonSerialized]
        public float[] Embedding;

        // Convenience property so call sites keep working with DateTime.
        public DateTime Timestamp
        {
            get => new DateTime(TimestampTicks, DateTimeKind.Utc);
            set => TimestampTicks = value.Ticks;
        }
    }
}
