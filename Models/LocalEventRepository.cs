using System;
using System.Collections.Generic;
using System.Linq;

namespace Municipal_App.Models
{
    /// <summary>
    /// In-memory repository for local events and announcements. Demonstrates use of several
    /// core data structures side by side:
    ///   • SortedDictionary&lt;DateTime, List&lt;EventItem&gt;&gt; — events auto-organised chronologically.
    ///   • Dictionary&lt;string, List&lt;EventItem&gt;&gt;          — hash-table lookup by category.
    ///   • HashSet&lt;string&gt; / HashSet&lt;DateTime&gt;           — unique categories / unique dates.
    ///   • Queue&lt;EventItem&gt;                                   — upcoming events in FIFO order.
    ///   • SimplePriorityQueue&lt;EventItem&gt;                     — featured/urgent events ranked first.
    ///   • Stack&lt;string&gt;                                      — most-recent search history (LIFO).
    /// plus a lightweight recommendation engine driven by search-frequency tracking
    /// (Dictionary&lt;string, int&gt;).
    /// </summary>
    public static class LocalEventRepository
    {
        // ── Core stores ─────────────────────────────────────────────────────
        private static readonly SortedDictionary<DateTime, List<EventItem>> _eventsByDate
            = new SortedDictionary<DateTime, List<EventItem>>();

        private static readonly Dictionary<string, List<EventItem>> _eventsByCategory
            = new Dictionary<string, List<EventItem>>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> _categories
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<DateTime> _uniqueDates = new HashSet<DateTime>();

        // ── Search / recommendation tracking ───────────────────────────────
        private static readonly Stack<string> _recentSearches = new Stack<string>();

        private static readonly Dictionary<string, int> _categorySearchCounts
            = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<int> _recommendedEventIds = new HashSet<int>();

        private static int _nextId = 1;
        private static bool _seeded;

        // ── Seed data ───────────────────────────────────────────────────────

        public static void EnsureSeeded()
        {
            if (_seeded) return;
            _seeded = true;

            DateTime today = DateTime.Today;

            AddEvent("Community Clean-Up Drive", "Environment",
                "Join your neighbours for a morning of litter-picking and recycling awareness along the riverside.",
                today.AddDays(2).AddHours(8), "Riverside Park", true);

            AddEvent("Municipal Budget Public Meeting", "Governance",
                "Council presents the draft annual budget and invites public comment.",
                today.AddDays(4).AddHours(18), "Town Hall, Main Chamber", true);

            AddEvent("Water Conservation Workshop", "Environment",
                "Learn practical tips to reduce household water usage during the dry season.",
                today.AddDays(6).AddHours(10), "Ward 12 Community Centre", false);

            AddEvent("Youth Sports Day", "Recreation",
                "A day of free sporting activities for children and teenagers, sponsored by the municipality.",
                today.AddDays(9).AddHours(9), "Greenfield Sports Grounds", true);

            AddEvent("Load-Shedding Schedule Briefing", "Utilities",
                "Information session on the updated electricity load-shedding schedule for the coming quarter.",
                today.AddDays(3).AddHours(17), "Municipal Offices, Hall B", false);

            AddEvent("Road Resurfacing Notice: Church Street", "Roads",
                "Church Street will be partially closed for resurfacing works.",
                today.AddDays(1).AddHours(7), "Church Street", false);

            AddEvent("Senior Citizens Wellness Day", "Health",
                "Free health screenings and wellness talks for residents 60 and older.",
                today.AddDays(11).AddHours(9), "Civic Centre Hall", true);

            AddEvent("Recycling & Waste Sorting Info Session", "Environment",
                "Understand the new colour-coded bin system being rolled out municipality-wide.",
                today.AddDays(14).AddHours(14), "Library Auditorium", false);

            AddEvent("Local Farmers Market", "Community",
                "Weekly market featuring local produce, crafts, and live music.",
                today.AddDays(5).AddHours(8), "Central Square", true);

            AddEvent("Public Safety & Crime Prevention Forum", "Safety",
                "Community policing forum discussing neighbourhood safety initiatives.",
                today.AddDays(7).AddHours(18), "Ward 4 Community Hall", false);

            AddEvent("Storm Drain Maintenance Notice", "Utilities",
                "Scheduled maintenance of storm drains ahead of the rainy season.",
                today.AddDays(2).AddHours(9), "Various Locations", false);

            AddEvent("Heritage Day Celebration", "Community",
                "Cultural performances, food stalls, and heritage exhibitions.",
                today.AddDays(20).AddHours(10), "Heritage Square", true);
        }

        private static void AddEvent(string title, string category, string description, DateTime date, string location, bool featured)
        {
            var evt = new EventItem
            {
                Id = _nextId++,
                Title = title,
                Category = category,
                Description = description,
                Date = date,
                Location = location,
                IsFeatured = featured
            };

            // SortedDictionary keeps entries auto-sorted by their DateTime key at all times.
            if (!_eventsByDate.TryGetValue(evt.Date, out List<EventItem> dateList))
            {
                dateList = new List<EventItem>();
                _eventsByDate[evt.Date] = dateList;
            }
            dateList.Add(evt);

            // Dictionary gives O(1) average lookup by category (a hash table).
            if (!_eventsByCategory.TryGetValue(evt.Category, out List<EventItem> catList))
            {
                catList = new List<EventItem>();
                _eventsByCategory[evt.Category] = catList;
            }
            catList.Add(evt);

            _categories.Add(evt.Category);       // HashSet: unique categories
            _uniqueDates.Add(evt.Date.Date);     // HashSet: unique calendar dates
        }

        // ── Reads ───────────────────────────────────────────────────────────

        /// <summary>All events, chronologically ordered (walks the SortedDictionary's keys).</summary>
        public static List<EventItem> GetAllSortedByDate()
        {
            EnsureSeeded();
            return _eventsByDate.Values.SelectMany(list => list).ToList();
        }

        /// <summary>Unique category names, materialised from the HashSet&lt;string&gt;.</summary>
        public static List<string> GetCategories()
        {
            EnsureSeeded();
            var list = new List<string>(_categories);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        /// <summary>Unique event dates, materialised from the HashSet&lt;DateTime&gt;.</summary>
        public static List<DateTime> GetUniqueDates()
        {
            EnsureSeeded();
            var list = new List<DateTime>(_uniqueDates);
            list.Sort();
            return list;
        }

        /// <summary>The next N upcoming (not-yet-passed) events, drained from a FIFO Queue.</summary>
        public static List<EventItem> GetUpcomingQueue(int count)
        {
            EnsureSeeded();
            var queue = new Queue<EventItem>(GetAllSortedByDate().Where(e => e.Date >= DateTime.Now));

            var result = new List<EventItem>();
            while (queue.Count > 0 && result.Count < count)
                result.Add(queue.Dequeue());
            return result;
        }

        /// <summary>
        /// Featured/urgent events ranked highest via a priority queue: featured events are
        /// always dequeued before non-featured ones, and within each group the soonest event
        /// comes first.
        /// </summary>
        public static List<EventItem> GetPriorityHighlights(int count)
        {
            EnsureSeeded();
            var pq = BuildPriorityQueue(GetAllSortedByDate().Where(e => e.Date >= DateTime.Now));

            var result = new List<EventItem>();
            while (!pq.IsEmpty && result.Count < count)
                result.Add(pq.Dequeue());
            return result;
        }

        private static SimplePriorityQueue<EventItem> BuildPriorityQueue(IEnumerable<EventItem> events)
        {
            var pq = new SimplePriorityQueue<EventItem>();
            foreach (EventItem evt in events)
            {
                int urgencyHours = (int)(evt.Date - DateTime.Now).TotalHours;
                // Featured events get a large head start (offset 0 vs 100000) so they always
                // outrank non-featured ones; within each tier, smaller urgency (sooner) wins.
                int priority = (evt.IsFeatured ? 0 : 100000) + urgencyHours;
                pq.Enqueue(evt, priority);
            }
            return pq;
        }

        // ── Search ──────────────────────────────────────────────────────────

        /// <summary>
        /// Filters events by keyword / category / date and records the search for the
        /// recommendation engine (Stack of recent searches + category frequency Dictionary).
        /// </summary>
        public static List<EventItem> Search(string keyword, string category, DateTime? date)
        {
            EnsureSeeded();
            IEnumerable<EventItem> results = GetAllSortedByDate();

            bool hasCategory = !string.IsNullOrWhiteSpace(category) && category != "All Categories";
            if (hasCategory)
                results = results.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));

            if (date.HasValue)
                results = results.Where(e => e.Date.Date == date.Value.Date);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string k = keyword.Trim();
                results = results.Where(e =>
                    e.Title.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Description.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Location.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var list = results.ToList();

            // Track the category behind this search so future recommendations can favour it.
            string trackedCategory = hasCategory ? category : list.Select(e => e.Category).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(trackedCategory))
                RecordSearch(trackedCategory);

            return list;
        }

        private static void RecordSearch(string category)
        {
            _recentSearches.Push(category); // Stack: most recent search ends up on top (LIFO)

            if (_categorySearchCounts.ContainsKey(category))
                _categorySearchCounts[category]++;
            else
                _categorySearchCounts[category] = 1;
        }

        /// <summary>The most recent N searched categories, top-of-stack (most recent) first.</summary>
        public static List<string> GetRecentSearches(int count)
        {
            return _recentSearches.Take(count).ToList();
        }

        /// <summary>
        /// Recommends upcoming events from whichever categories the user has searched most
        /// often, ranked via the same priority scheme as highlights (featured first, then
        /// soonest), while avoiding events already surfaced as recommendations this session.
        /// Falls back to general highlights if there is no search history yet.
        /// </summary>
        public static List<EventItem> GetRecommendations(int count)
        {
            EnsureSeeded();

            if (_categorySearchCounts.Count == 0)
                return GetPriorityHighlights(count);

            List<string> topCategories = _categorySearchCounts
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .Take(3)
                .ToList();

            var candidateEvents = new List<EventItem>();
            foreach (string cat in topCategories)
            {
                if (_eventsByCategory.TryGetValue(cat, out List<EventItem> events))
                    candidateEvents.AddRange(events.Where(e => e.Date >= DateTime.Now));
            }

            var pq = BuildPriorityQueue(candidateEvents);

            var result = new List<EventItem>();
            while (!pq.IsEmpty && result.Count < count)
            {
                EventItem evt = pq.Dequeue();
                if (_recommendedEventIds.Contains(evt.Id)) continue;
                result.Add(evt);
            }

            // Top up with general highlights if the searched categories didn't yield enough.
            if (result.Count < count)
            {
                foreach (EventItem evt in GetPriorityHighlights(count))
                {
                    if (result.Count >= count) break;
                    if (result.Any(r => r.Id == evt.Id)) continue;
                    result.Add(evt);
                }
            }

            foreach (EventItem evt in result) _recommendedEventIds.Add(evt.Id);
            return result;
        }
    }
}
