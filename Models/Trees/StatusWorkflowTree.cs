using System.Collections.Generic;
using Municipal_App.Models;

namespace Municipal_App.Models.Trees
{
    /// <summary>
    /// A fixed binary tree (each node has at most two children) that models the valid
    /// status transitions a service request can move through. This is a genuine "basic
    /// tree" / "binary tree" structure (as distinct from the balanced search trees used
    /// elsewhere): it is built once, is not keyed/searched by value, and its purpose is
    /// purely hierarchical — showing which status(es) can follow a given one, plus
    /// standard tree traversals.
    /// </summary>
    public static class StatusWorkflowTree
    {
        public class WorkflowNode
        {
            public ServiceRequestStatus Status;
            public WorkflowNode Left;  // first/typical next step
            public WorkflowNode Right; // alternate next step
            public WorkflowNode(ServiceRequestStatus status) { Status = status; }
        }

        private static readonly WorkflowNode Root;

        static StatusWorkflowTree()
        {
            // Submitted -> Investigating (typical) or Closed (rejected/duplicate)
            // Investigating -> InProgress (typical) or Closed (no action needed)
            // InProgress -> Scheduled (typical) or Resolved (fixed immediately)
            // Scheduled -> Resolved (typical) or InProgress (rescheduled/reopened)
            Root = new WorkflowNode(ServiceRequestStatus.Submitted)
            {
                Left = new WorkflowNode(ServiceRequestStatus.Investigating)
                {
                    Left = new WorkflowNode(ServiceRequestStatus.InProgress)
                    {
                        Left = new WorkflowNode(ServiceRequestStatus.Scheduled)
                        {
                            Left = new WorkflowNode(ServiceRequestStatus.Resolved)
                            {
                                Left = new WorkflowNode(ServiceRequestStatus.Closed)
                            },
                            Right = new WorkflowNode(ServiceRequestStatus.InProgress)
                        },
                        Right = new WorkflowNode(ServiceRequestStatus.Resolved)
                    },
                    Right = new WorkflowNode(ServiceRequestStatus.Closed)
                },
                Right = new WorkflowNode(ServiceRequestStatus.Closed)
            };
        }

        /// <summary>Finds the single node in the tree matching the given status (first match, pre-order).</summary>
        public static WorkflowNode FindNode(ServiceRequestStatus status)
        {
            return FindNode(Root, status);
        }

        private static WorkflowNode FindNode(WorkflowNode node, ServiceRequestStatus status)
        {
            if (node == null) return null;
            if (node.Status == status) return node;
            return FindNode(node.Left, status) ?? FindNode(node.Right, status);
        }

        /// <summary>The immediate possible next status(es) reachable from the given status.</summary>
        public static List<ServiceRequestStatus> GetNextSteps(ServiceRequestStatus status)
        {
            var node = FindNode(status);
            var result = new List<ServiceRequestStatus>();
            if (node?.Left != null) result.Add(node.Left.Status);
            if (node?.Right != null) result.Add(node.Right.Status);
            return result;
        }

        /// <summary>Pre-order traversal (root, left, right) of the whole workflow tree.</summary>
        public static List<ServiceRequestStatus> PreOrder()
        {
            var result = new List<ServiceRequestStatus>();
            PreOrder(Root, result);
            return result;
        }

        private static void PreOrder(WorkflowNode node, List<ServiceRequestStatus> result)
        {
            if (node == null) return;
            result.Add(node.Status);
            PreOrder(node.Left, result);
            PreOrder(node.Right, result);
        }

        /// <summary>Level-order (breadth-first) traversal of the whole workflow tree.</summary>
        public static List<ServiceRequestStatus> LevelOrder()
        {
            var result = new List<ServiceRequestStatus>();
            var queue = new Queue<WorkflowNode>();
            queue.Enqueue(Root);
            while (queue.Count > 0)
            {
                WorkflowNode node = queue.Dequeue();
                result.Add(node.Status);
                if (node.Left != null) queue.Enqueue(node.Left);
                if (node.Right != null) queue.Enqueue(node.Right);
            }
            return result;
        }
    }
}
