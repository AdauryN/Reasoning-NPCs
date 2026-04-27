using System;
using System.Collections.Generic;

namespace NPC_AI.PlayerAnalysis
{
    /// Fixed capacity ring buffer. Overwrites the oldest entry when full.
    /// Used by PlayerBehaviorTracker to keep the last behavior events.
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _count;

        public int Count => _count;
        public int Capacity => _buffer.Length;

        public CircularBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = new T[capacity];
        }

        public void Push(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
        }

        /// Returns all stored items, oldest first.
        public T[] ToArray()
        {
            if (_count == 0) return Array.Empty<T>();
            var result = new T[_count];
            int start = _count < _buffer.Length ? 0 : _head;
            for (int i = 0; i < _count; i++)
                result[i] = _buffer[(start + i) % _buffer.Length];
            return result;
        }
    }
}
