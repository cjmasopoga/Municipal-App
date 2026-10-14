using System;

namespace Municipal_App.Models
{
    /// <summary>Represents a single local municipal event or announcement.</summary>
    public class EventItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }

        /// <summary>Featured events are surfaced first in highlights/recommendations.</summary>
        public bool IsFeatured { get; set; }
    }
}
