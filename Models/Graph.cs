using System;
using System.Collections.Generic;

namespace Municipal_App.Models
{
    /// <summary>
    /// A generic undirected, weighted graph (adjacency-list representation). Used to
    /// model relationships between service requests (e.g. shared category/location) so
    /// that related requests can be discovered via traversal, and an efficient service
    /// network can be computed via a Minimum Spanning Tree.
    /// </summary>
    public class Graph<T>
    {
        public struct Edge
        {
            public T To;
            public double Weight;
            public Edge(T to, double weight) { To = to; Weight = weight; }
        }

        private readonly Dictionary<T, List<Edge>> _adjacency = new Dictionary<T, List<Edge>>();

        public int VertexCount => _adjacency.Count;
        public IEnumerable<T> Vertices => _adjacency.Keys;

        public void AddVertex(T vertex)
        {
            if (!_adjacency.ContainsKey(vertex))
                _adjacency[vertex] = new List<Edge>();
        }

        public void AddEdge(T a, T b, double weight)
        {
            AddVertex(a);
            AddVertex(b);
            _adjacency[a].Add(new Edge(b, weight));
            _adjacency[b].Add(new Edge(a, weight));
        }

        public IReadOnlyList<Edge> NeighboursOf(T vertex)
        {
            return _adjacency.TryGetValue(vertex, out List<Edge> list) ? list : new List<Edge>();
        }

        /// <summary>Breadth-first traversal of the whole connected component containing <paramref name="start"/>.</summary>
        public List<T> Bfs(T start)
        {
            return BfsLimited(start, int.MaxValue, null, includeStart: true);
        }

        /// <summary>Depth-first traversal (iterative, stack-based) of the connected component containing <paramref name="start"/>.</summary>
        public List<T> Dfs(T start)
        {
            var visited = new HashSet<T>();
            var order = new List<T>();
            if (!_adjacency.ContainsKey(start)) return order;

            var stack = new Stack<T>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                T current = stack.Pop();
                if (!visited.Add(current)) continue;
                order.Add(current);
                foreach (Edge edge in _adjacency[current])
                    if (!visited.Contains(edge.To))
                        stack.Push(edge.To);
            }
            return order;
        }

        /// <summary>
        /// Breadth-first search limited to <paramref name="maxHops"/> hops, optionally only
        /// following edges that pass <paramref name="edgeFilter"/>. Used to find "closely
        /// related" vertices rather than the entire connected component.
        /// </summary>
        public List<T> BfsLimited(T start, int maxHops, Func<Edge, bool> edgeFilter, bool includeStart = false)
        {
            var result = new List<T>();
            if (!_adjacency.ContainsKey(start)) return result;

            var visited = new HashSet<T> { start };
            var queue = new Queue<(T Node, int Hop)>();
            queue.Enqueue((start, 0));
            if (includeStart) result.Add(start);

            while (queue.Count > 0)
            {
                (T node, int hop) = queue.Dequeue();
                if (hop >= maxHops) continue;

                foreach (Edge edge in _adjacency[node])
                {
                    if (edgeFilter != null && !edgeFilter(edge)) continue;
                    if (!visited.Add(edge.To)) continue;

                    result.Add(edge.To);
                    queue.Enqueue((edge.To, hop + 1));
                }
            }
            return result;
        }

        /// <summary>
        /// Computes a Minimum Spanning Tree for the connected component containing
        /// <paramref name="start"/> using Prim's algorithm. A <see cref="SimplePriorityQueue{T}"/>
        /// (binary heap) selects the cheapest frontier edge at each step, giving the
        /// classic O(E log V) greedy MST construction.
        /// </summary>
        public List<(T From, T To, double Weight)> MinimumSpanningTree(T start)
        {
            var result = new List<(T From, T To, double Weight)>();
            if (!_adjacency.ContainsKey(start)) return result;

            var visited = new HashSet<T> { start };
            var frontier = new SimplePriorityQueue<(T From, T To, double Weight)>();

            void EnqueueEdgesFrom(T vertex)
            {
                foreach (Edge edge in _adjacency[vertex])
                    if (!visited.Contains(edge.To))
                        frontier.Enqueue((vertex, edge.To, edge.Weight), (int)Math.Round(edge.Weight * 1000));
            }

            EnqueueEdgesFrom(start);

            while (!frontier.IsEmpty && visited.Count < _adjacency.Count)
            {
                (T from, T to, double weight) = frontier.Dequeue();
                if (visited.Contains(to)) continue;

                visited.Add(to);
                result.Add((from, to, weight));
                EnqueueEdgesFrom(to);
            }
            return result;
        }
    }
}
