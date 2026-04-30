using System;
using System.Collections.Generic;
using System.Linq;

namespace NPC_AI.RAG
{
    /// <summary>
    /// Brute-force cosine similarity index for NPC-scale memory (~hundreds of entries).
    /// At this scale, brute-force search over float arrays is faster than HNSW overhead.
    /// </summary>
    public class VectorIndex<T>
    {
        private readonly List<(float[] Vector, T Item)> _items = new List<(float[], T)>();

        public int Count => _items.Count;

        public void Add(float[] vector, T item)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            _items.Add((vector, item));
        }

        public void Remove(T item)
        {
            _items.RemoveAll(x => EqualityComparer<T>.Default.Equals(x.Item, item));
        }

        public void Clear() => _items.Clear();

        /// <summary>
        /// Returns the top-K items ranked by cosine similarity to <paramref name="queryVector"/>.
        /// </summary>
        public List<T> Search(float[] queryVector, int topK)
        {
            if (queryVector == null || _items.Count == 0) return new List<T>();

            return _items
                .Select(entry => (score: CosineSimilarity(entry.Vector, queryVector), entry.Item))
                .OrderByDescending(x => x.score)
                .Take(topK)
                .Select(x => x.Item)
                .ToList();
        }

        private static float CosineSimilarity(float[] a, float[] b)
        {
            int len = Math.Min(a.Length, b.Length);
            float dot = 0f, magA = 0f, magB = 0f;
            for (int i = 0; i < len; i++)
            {
                dot  += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            float denom = MathF.Sqrt(magA) * MathF.Sqrt(magB);
            return denom < float.Epsilon ? 0f : dot / denom;
        }
    }
}
