using System;
using System.Collections.Generic;

namespace Municipal_App.Models.Trees
{
    /// <summary>
    /// A red-black tree: a self-balancing binary search tree that guarantees O(log n)
    /// search/insert by maintaining five colour invariants (root is black, red nodes
    /// never have red children, every root-to-leaf path has the same number of black
    /// nodes, etc.), restored after each insert via recolouring and rotations. Used by
    /// the repository to retrieve service requests in chronological (submission date)
    /// order with guaranteed balance.
    /// </summary>
    public class RedBlackTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private enum NodeColor { Red, Black }

        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Left, Right, Parent;
            public NodeColor Color = NodeColor.Red;
            public Node(TKey key, TValue value) { Key = key; Value = value; }
        }

        // Sentinel "nil" leaf — simplifies boundary checks (CLRS-style implementation).
        private readonly Node _nil;
        private Node _root;

        public int Count { get; private set; }

        public RedBlackTree()
        {
            _nil = new Node(default(TKey), default(TValue)) { Color = NodeColor.Black };
            _nil.Left = _nil.Right = _nil.Parent = _nil;
            _root = _nil;
        }

        private void LeftRotate(Node x)
        {
            Node y = x.Right;
            x.Right = y.Left;
            if (y.Left != _nil) y.Left.Parent = x;
            y.Parent = x.Parent;

            if (x.Parent == _nil) _root = y;
            else if (x == x.Parent.Left) x.Parent.Left = y;
            else x.Parent.Right = y;

            y.Left = x;
            x.Parent = y;
        }

        private void RightRotate(Node x)
        {
            Node y = x.Left;
            x.Left = y.Right;
            if (y.Right != _nil) y.Right.Parent = x;
            y.Parent = x.Parent;

            if (x.Parent == _nil) _root = y;
            else if (x == x.Parent.Right) x.Parent.Right = y;
            else x.Parent.Left = y;

            y.Right = x;
            x.Parent = y;
        }

        public void Insert(TKey key, TValue value)
        {
            Node y = _nil;
            Node x = _root;

            while (x != _nil)
            {
                y = x;
                int cmp = key.CompareTo(x.Key);
                if (cmp == 0) { x.Value = value; return; } // key already present: update in place
                x = cmp < 0 ? x.Left : x.Right;
            }

            var z = new Node(key, value) { Left = _nil, Right = _nil, Parent = y };

            if (y == _nil) _root = z;
            else if (key.CompareTo(y.Key) < 0) y.Left = z;
            else y.Right = z;

            Count++;
            InsertFixup(z);
        }

        private void InsertFixup(Node z)
        {
            while (z.Parent.Color == NodeColor.Red)
            {
                if (z.Parent == z.Parent.Parent.Left)
                {
                    Node uncle = z.Parent.Parent.Right;
                    if (uncle.Color == NodeColor.Red)
                    {
                        z.Parent.Color = NodeColor.Black;
                        uncle.Color = NodeColor.Black;
                        z.Parent.Parent.Color = NodeColor.Red;
                        z = z.Parent.Parent;
                    }
                    else
                    {
                        if (z == z.Parent.Right)
                        {
                            z = z.Parent;
                            LeftRotate(z);
                        }
                        z.Parent.Color = NodeColor.Black;
                        z.Parent.Parent.Color = NodeColor.Red;
                        RightRotate(z.Parent.Parent);
                    }
                }
                else
                {
                    Node uncle = z.Parent.Parent.Left;
                    if (uncle.Color == NodeColor.Red)
                    {
                        z.Parent.Color = NodeColor.Black;
                        uncle.Color = NodeColor.Black;
                        z.Parent.Parent.Color = NodeColor.Red;
                        z = z.Parent.Parent;
                    }
                    else
                    {
                        if (z == z.Parent.Left)
                        {
                            z = z.Parent;
                            RightRotate(z);
                        }
                        z.Parent.Color = NodeColor.Black;
                        z.Parent.Parent.Color = NodeColor.Red;
                        LeftRotate(z.Parent.Parent);
                    }
                }
            }
            _root.Color = NodeColor.Black;
        }

        public bool TryFind(TKey key, out TValue value)
        {
            Node current = _root;
            while (current != _nil)
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
            if (node == _nil) return;
            InOrder(node.Left, result);
            result.Add(node.Value);
            InOrder(node.Right, result);
        }
    }
}
