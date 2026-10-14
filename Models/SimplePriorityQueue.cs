using System;
using System.Collections.Generic;

namespace Municipal_App.Models
{
    /// <summary>
    /// A minimal binary-heap-based priority queue (lower priority value is dequeued first).
    /// .NET Framework 4.7.2 does not ship a built-in PriorityQueue&lt;T&gt; (that type only
    /// arrived in .NET 6), so this provides an equivalent used to rank events by
    /// urgency/featured status when building highlights and recommendations.
    /// </summary>
    public class SimplePriorityQueue<T>
    {
        private readonly List<(int Priority, T Item)> _heap = new List<(int, T)>();

        public int Count => _heap.Count;
        public bool IsEmpty => _heap.Count == 0;

        public void Enqueue(T item, int priority)
        {
            _heap.Add((priority, item));
            int i = _heap.Count - 1;

            // Bubble the new entry up until the min-heap property is restored.
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_heap[parent].Priority <= _heap[i].Priority) break;
                Swap(parent, i);
                i = parent;
            }
        }

        public T Dequeue()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("Priority queue is empty.");

            (int Priority, T Item) root = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            // Sift the replacement root down until the min-heap property is restored.
            int i = 0;
            while (true)
            {
                int left = i * 2 + 1;
                int right = i * 2 + 2;
                int smallest = i;

                if (left < _heap.Count && _heap[left].Priority < _heap[smallest].Priority) smallest = left;
                if (right < _heap.Count && _heap[right].Priority < _heap[smallest].Priority) smallest = right;
                if (smallest == i) break;

                Swap(i, smallest);
                i = smallest;
            }

            return root.Item;
        }

        private void Swap(int a, int b)
        {
            (int Priority, T Item) temp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = temp;
        }
    }
}
