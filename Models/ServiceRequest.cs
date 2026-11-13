using System;

namespace Municipal_App.Models
{
    /// <summary>Priority levels for a service request. Higher numeric value = more urgent.</summary>
    public enum RequestPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Status progression a request moves through. The valid transitions between these
    /// values are modelled as a binary tree in <see cref="Trees.StatusWorkflowTree"/>.
    /// </summary>
    public enum ServiceRequestStatus
    {
        Submitted,
        Investigating,
        InProgress,
        Scheduled,
        Resolved,
        Closed
    }

    /// <summary>Represents a single municipal service request and its tracked status.</summary>
    public class ServiceRequest
    {
        public int Id { get; set; }

        /// <summary>The unique, citizen-facing identifier (e.g. "SR-1004") used to track progress.</summary>
        public string ReferenceNumber { get; set; }

        public string Title { get; set; }
        public string Category { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public RequestPriority Priority { get; set; }
        public ServiceRequestStatus Status { get; set; }
        public DateTime DateSubmitted { get; set; }
        public DateTime LastUpdated { get; set; }
        public string AssignedTeam { get; set; }

        public int DaysOpen => (int)(DateTime.Now - DateSubmitted).TotalDays;
    }
}
