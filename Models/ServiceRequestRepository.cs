using System;
using System.Collections.Generic;
using System.Linq;
using Municipal_App.Models.Trees;

namespace Municipal_App.Models
{
    /// <summary>
    /// In-memory repository for service requests. Demonstrates several tree and graph
    /// structures operating side by side over the same underlying data:
    ///   • BinarySearchTree&lt;string, ServiceRequest&gt;  — lookup by unique reference number.
    ///   • AvlTree&lt;PriorityKey, ServiceRequest&gt;        — balanced retrieval ordered by priority.
    ///   • RedBlackTree&lt;DateKey, ServiceRequest&gt;       — balanced retrieval ordered by date.
    ///   • SimplePriorityQueue&lt;ServiceRequest&gt; (heap)  — "most urgent" ranking.
    ///   • Graph&lt;int&gt;                                  — relatedness network, traversal, and MST.
    ///   • StatusWorkflowTree (binary tree)              — valid status transitions.
    /// </summary>
    public static class ServiceRequestRepository
    {
        /// <summary>Composite key (priority, then id) so the AVL tree can hold many requests of equal priority.</summary>
        private struct PriorityKey : IComparable<PriorityKey>
        {
            // Stored negated so ascending in-order traversal yields highest-priority first.
            public int SortPriority;
            public int Id;
            public PriorityKey(RequestPriority priority, int id) { SortPriority = -(int)priority; Id = id; }

            public int CompareTo(PriorityKey other)
            {
                int cmp = SortPriority.CompareTo(other.SortPriority);
                return cmp != 0 ? cmp : Id.CompareTo(other.Id);
            }
        }

        /// <summary>Composite key (date ticks, then id) so the red-black tree can hold many requests on the same date.</summary>
        private struct DateKey : IComparable<DateKey>
        {
            public long Ticks;
            public int Id;
            public DateKey(DateTime date, int id) { Ticks = date.Ticks; Id = id; }

            public int CompareTo(DateKey other)
            {
                int cmp = Ticks.CompareTo(other.Ticks);
                return cmp != 0 ? cmp : Id.CompareTo(other.Id);
            }
        }

        private static readonly List<ServiceRequest> _all = new List<ServiceRequest>();
        private static readonly BinarySearchTree<string, ServiceRequest> _byReference = new BinarySearchTree<string, ServiceRequest>();
        private static readonly AvlTree<PriorityKey, ServiceRequest> _byPriority = new AvlTree<PriorityKey, ServiceRequest>();
        private static readonly RedBlackTree<DateKey, ServiceRequest> _byDate = new RedBlackTree<DateKey, ServiceRequest>();
        private static readonly Graph<int> _relationshipGraph = new Graph<int>();
        private static readonly Dictionary<int, ServiceRequest> _byId = new Dictionary<int, ServiceRequest>();

        private static int _nextId = 1001;
        private static bool _seeded;

        public static void EnsureSeeded()
        {
            if (_seeded) return;
            _seeded = true;

            DateTime today = DateTime.Today;

            Add("Burst water pipe flooding sidewalk", "Water & Sanitation", "12 Market Street",
                "A burst municipal water pipe is flooding the sidewalk and nearby driveways.",
                RequestPriority.Critical, ServiceRequestStatus.InProgress, today.AddDays(-1), "Water & Sanitation Crew A");

            Add("Pothole causing traffic hazard", "Roads & Pavements", "Church Street",
                "A deep pothole near the Church Street intersection is damaging vehicles.",
                RequestPriority.High, ServiceRequestStatus.Scheduled, today.AddDays(-5), "Roads Maintenance Team 2");

            Add("Streetlight out for a week", "Electricity & Street Lights", "12 Market Street",
                "The streetlight outside 12 Market Street has been out for a week, a safety concern at night.",
                RequestPriority.Medium, ServiceRequestStatus.Investigating, today.AddDays(-7), "Electrical Services");

            Add("Overflowing refuse bins", "Refuse & Waste Collection", "Greenfield Sports Grounds",
                "Refuse bins near the sports grounds have not been collected in two weeks.",
                RequestPriority.Medium, ServiceRequestStatus.Submitted, today.AddDays(-2), "Unassigned");

            Add("Broken playground equipment", "Parks & Recreation", "Riverside Park",
                "Swing set in Riverside Park is broken and poses an injury risk to children.",
                RequestPriority.High, ServiceRequestStatus.Scheduled, today.AddDays(-4), "Parks Maintenance");

            Add("Bus stop shelter vandalised", "Public Transport", "Central Square",
                "The bus shelter at Central Square has broken glass and needs repair.",
                RequestPriority.Low, ServiceRequestStatus.Submitted, today.AddDays(-1), "Unassigned");

            Add("Illegal dumping / noise complaint", "Noise & Environmental", "Ward 12 Community Centre",
                "Repeated illegal dumping and late-night noise disturbances reported near the community centre.",
                RequestPriority.Medium, ServiceRequestStatus.Investigating, today.AddDays(-6), "Environmental Health Unit");

            Add("Unsafe scaffolding on construction site", "Building & Safety", "Heritage Square",
                "Scaffolding outside a construction site on Heritage Square appears structurally unsafe.",
                RequestPriority.Critical, ServiceRequestStatus.InProgress, today.AddDays(-3), "Building Inspectorate");

            Add("Water pressure extremely low", "Water & Sanitation", "Ward 4 Community Hall",
                "Residents around Ward 4 Community Hall report very low water pressure for several days.",
                RequestPriority.High, ServiceRequestStatus.Investigating, today.AddDays(-2), "Water & Sanitation Crew B");

            Add("Road resurfacing request", "Roads & Pavements", "Greenfield Sports Grounds",
                "Road surface near the sports grounds has multiple cracks and needs resurfacing.",
                RequestPriority.Low, ServiceRequestStatus.Submitted, today.AddDays(-10), "Unassigned");

            Add("Traffic light malfunction", "Electricity & Street Lights", "Church Street",
                "Traffic light at the Church Street intersection is stuck on red in all directions.",
                RequestPriority.Critical, ServiceRequestStatus.Resolved, today.AddDays(-9), "Electrical Services");

            Add("Missed refuse collection", "Refuse & Waste Collection", "12 Market Street",
                "Weekly refuse collection was missed on Market Street for the second time this month.",
                RequestPriority.Low, ServiceRequestStatus.Closed, today.AddDays(-14), "Waste Collection Team 1");

            Add("Park irrigation system leaking", "Parks & Recreation", "Riverside Park",
                "An underground irrigation pipe in Riverside Park is leaking, creating a muddy hazard.",
                RequestPriority.Medium, ServiceRequestStatus.Scheduled, today.AddDays(-8), "Parks Maintenance");

            Add("Storm drain blocked causing flooding", "Water & Sanitation", "Heritage Square",
                "A blocked storm drain at Heritage Square is causing flooding after rain.",
                RequestPriority.High, ServiceRequestStatus.Submitted, today.AddDays(-1), "Unassigned");

            BuildRelationshipGraph();
        }

        private static void Add(string title, string category, string location, string description,
            RequestPriority priority, ServiceRequestStatus status, DateTime dateSubmitted, string assignedTeam)
        {
            int id = _nextId++;
            var request = new ServiceRequest
            {
                Id = id,
                ReferenceNumber = "SR-" + id,
                Title = title,
                Category = category,
                Location = location,
                Description = description,
                Priority = priority,
                Status = status,
                DateSubmitted = dateSubmitted,
                LastUpdated = dateSubmitted.AddDays(Math.Min((DateTime.Now - dateSubmitted).Days, 2)),
                AssignedTeam = assignedTeam
            };

            _all.Add(request);
            _byId[request.Id] = request;
            _byReference.Insert(request.ReferenceNumber, request);
            _byPriority.Insert(new PriorityKey(request.Priority, request.Id), request);
            _byDate.Insert(new DateKey(request.DateSubmitted, request.Id), request);
            _relationshipGraph.AddVertex(request.Id);
        }

        /// <summary>
        /// Builds the relatedness graph: a low-weight "baseline chain" guarantees the graph
        /// stays connected, while extra low-weight edges between requests sharing a category
        /// or location pull genuinely related requests close together for MST/traversal.
        /// </summary>
        private static void BuildRelationshipGraph()
        {
            for (int i = 1; i < _all.Count; i++)
                _relationshipGraph.AddEdge(_all[i - 1].Id, _all[i].Id, 12.0);

            for (int i = 0; i < _all.Count; i++)
            {
                for (int j = i + 1; j < _all.Count; j++)
                {
                    ServiceRequest a = _all[i];
                    ServiceRequest b = _all[j];

                    bool sameCategory = string.Equals(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
                    bool sameLocation = string.Equals(a.Location, b.Location, StringComparison.OrdinalIgnoreCase);
                    if (!sameCategory && !sameLocation) continue;

                    double weight = 10.0;
                    if (sameCategory) weight -= 5.0;
                    if (sameLocation) weight -= 4.0;
                    weight += Math.Abs((a.DateSubmitted - b.DateSubmitted).TotalDays) * 0.2;
                    weight = Math.Max(weight, 0.5);

                    _relationshipGraph.AddEdge(a.Id, b.Id, weight);
                }
            }
        }

        // ── Reads ───────────────────────────────────────────────────────────

        public static List<ServiceRequest> GetAll()
        {
            EnsureSeeded();
            return new List<ServiceRequest>(_all);
        }

        /// <summary>Tracks a request by its unique reference number via the binary search tree.</summary>
        public static ServiceRequest TrackByReference(string referenceNumber)
        {
            EnsureSeeded();
            if (string.IsNullOrWhiteSpace(referenceNumber)) return null;
            _byReference.TryFind(referenceNumber.Trim(), out ServiceRequest result);
            return result;
        }

        /// <summary>Highest priority first (AVL tree in-order traversal).</summary>
        public static List<ServiceRequest> GetByPriorityDescending()
        {
            EnsureSeeded();
            return _byPriority.InOrderValues();
        }

        /// <summary>Oldest first (red-black tree in-order traversal).</summary>
        public static List<ServiceRequest> GetChronological()
        {
            EnsureSeeded();
            return _byDate.InOrderValues();
        }

        /// <summary>
        /// The most urgent open requests, ranked via a priority queue (binary heap).
        /// Urgency favours higher priority, then longer time open.
        /// </summary>
        public static List<ServiceRequest> GetMostUrgent(int count)
        {
            EnsureSeeded();
            var heap = new SimplePriorityQueue<ServiceRequest>();
            foreach (ServiceRequest request in _all.Where(r => r.Status != ServiceRequestStatus.Resolved && r.Status != ServiceRequestStatus.Closed))
            {
                int urgency = -((int)request.Priority * 1000 + request.DaysOpen);
                heap.Enqueue(request, urgency);
            }

            var result = new List<ServiceRequest>();
            while (!heap.IsEmpty && result.Count < count)
                result.Add(heap.Dequeue());
            return result;
        }

        /// <summary>
        /// Requests closely related to the given one (shared category/location), found via a
        /// hop-and-weight-limited breadth-first search of the relationship graph.
        /// </summary>
        public static List<ServiceRequest> GetRelated(int id, int maxResults = 5)
        {
            EnsureSeeded();
            List<int> relatedIds = _relationshipGraph.BfsLimited(id, maxHops: 2, edgeFilter: e => e.Weight < 8.0);
            return relatedIds
                .Where(rid => rid != id && _byId.ContainsKey(rid))
                .Select(rid => _byId[rid])
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// The full relationship network reduced to its Minimum Spanning Tree — the
        /// cheapest set of connections that still links every service request, useful for
        /// visualising an efficient "coordination network" for field teams.
        /// </summary>
        public static List<(ServiceRequest From, ServiceRequest To, double Weight)> GetServiceNetworkMst()
        {
            EnsureSeeded();
            if (_all.Count == 0) return new List<(ServiceRequest, ServiceRequest, double)>();

            List<(int From, int To, double Weight)> mstEdges = _relationshipGraph.MinimumSpanningTree(_all[0].Id);
            return mstEdges.Select(e => (_byId[e.From], _byId[e.To], e.Weight)).ToList();
        }

        /// <summary>All distinct edges in the relationship graph (for visualising the full network alongside its MST).</summary>
        public static List<(ServiceRequest A, ServiceRequest B, double Weight)> GetAllRelationshipEdges()
        {
            EnsureSeeded();
            var seen = new HashSet<(int, int)>();
            var edges = new List<(ServiceRequest, ServiceRequest, double)>();

            foreach (int vertex in _relationshipGraph.Vertices)
            {
                foreach (Graph<int>.Edge edge in _relationshipGraph.NeighboursOf(vertex))
                {
                    int a = Math.Min(vertex, edge.To);
                    int b = Math.Max(vertex, edge.To);
                    if (seen.Add((a, b)))
                        edges.Add((_byId[a], _byId[b], edge.Weight));
                }
            }
            return edges;
        }

        /// <summary>Valid next status/statuses for a request, via the binary workflow tree.</summary>
        public static List<ServiceRequestStatus> GetNextSteps(ServiceRequestStatus current)
        {
            return StatusWorkflowTree.GetNextSteps(current);
        }

        public static List<string> GetCategories()
        {
            EnsureSeeded();
            return _all.Select(r => r.Category).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static int GetCount()
        {
            EnsureSeeded();
            return _all.Count;
        }
    }
}
