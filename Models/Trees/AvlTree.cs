using System;
using System.Collections.Generic;

namespace Municipal_App.Models.Trees
{
    /// <summary>
    /// A self-balancing AVL binary search tree. After every insert, rotations restore the
    /// AVL invariant (left/right subtree heights differ by at most 1), guaranteeing
    /// O(log n) search/insert even for sorted input — unlike a plain BST, which can
    /// degrade to a linked list. Used by the repository to retrieve service requests
    /// ordered by priority with a guaranteed-efficient structure.
    /// </summary>
    public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Left;
            public Node Right;
            public int Height = 1;
            public Node(TKey key, TValue value) { Key = key; Value = value; }
        }

        private Node _root;

        public int Count { get; private set; }

        private static int HeightOf(Node n) => n == null ? 0 : n.Height;
        private static int BalanceOf(Node n) => n == null ? 0 : HeightOf(n.Left) - HeightOf(n.Right);
        private static void RecalculateHeight(Node n) => n.Height = 1 + Math.Max(HeightOf(n.Left), HeightOf(n.Right));

        private static Node RotateRight(Node y)
        {
            Node x = y.Left;
            Node transferred = x.Right;

            x.Right = y;
            y.Left = transferred;

            RecalculateHeight(y);
            RecalculateHeight(x);
            return x;
        }

        private static Node RotateLeft(Node x)
        {
            Node y = x.Right;
            Node transferred = y.Left;

            y.Left = x;
            x.Right = transferred;

            RecalculateHeight(x);
            RecalculateHeight(y);
            return y;
        }

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
            else { node.Value = value; return node; }

            RecalculateHeight(node);
            int balance = BalanceOf(node);

            // Left-Left case
            if (balance > 1 && key.CompareTo(node.Left.Key) < 0)
                return RotateRight(node);

            // Right-Right case
            if (balance < -1 && key.CompareTo(node.Right.Key) > 0)
                return RotateLeft(node);

            // Left-Right case
            if (balance > 1 && key.CompareTo(node.Left.Key) > 0)
            {
                node.Left = RotateLeft(node.Left);
                return RotateRight(node);
            }

            // Right-Left case
            if (balance < -1 && key.CompareTo(node.Right.Key) < 0)
            {
                node.Right = RotateRight(node.Right);
                return RotateLeft(node);
            }

            return node;
        }

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
