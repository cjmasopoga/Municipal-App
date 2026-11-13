using System;
using System.Collections.Generic;

namespace Municipal_App.Models.Trees
{
    /// <summary>
    /// A standard (unbalanced) binary search tree. Each key appears at most once; keys
    /// smaller than a node go left, larger go right, giving O(log n) average-case
    /// search/insert. Used by the repository to track service requests by their unique
    /// reference number.
    /// </summary>
    public class BinarySearchTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Left;
            public Node Right;
            public Node(TKey key, TValue value) { Key = key; Value = value; }
        }

        private Node _root;

        public int Count { get; private set; }

        public void Insert(TKey key, TValue value)
        {
            _root = InsertNode(_root, key, value);
        }

        private Node InsertNode(Node node, TKey key, TValue value)
        {
            if (node == null)
            {
                Count++;
                return new Node(key, value);
            }

            int cmp = key.CompareTo(node.Key);
            if (cmp < 0) node.Left = InsertNode(node.Left, key, value);
            else if (cmp > 0) node.Right = InsertNode(node.Right, key, value);
            else node.Value = value; // key already present: update in place

            return node;
        }

        /// <summary>O(log n) average-case lookup by unique key.</summary>
        public bool TryFind(TKey key, out TValue value)
        {
            Node current = _root;
            while (current != null)
            {
                int cmp = key.CompareTo(current.Key);
                if (cmp == 0) { value = current.Value; return true; }
                current = cmp < 0 ? current.Left : current.Right;
            }
            value = default(TValue);
            return false;
        }

        /// <summary>Returns all values ordered ascending by key (in-order traversal).</summary>
        public List<TValue> InOrderValues()
        {
            var result = new List<TValue>();
            InOrder(_root, result);
            return result;
        }

        private void InOrder(Node node, List<TValue> result)
        {
            if (node == null) return;
            InOrder(node.Left, result);
            result.Add(node.Value);
            InOrder(node.Right, result);
        }
    }
}
