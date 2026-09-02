using System;
using System.Collections.Generic;
using System.Linq;

namespace Municipal_App.Models
{
    /// <summary>Represents a single citizen-reported municipal issue.</summary>
    public class IssueReport
    {
        public int    Id               { get; set; }
        public string Location         { get; set; }
        public string Category         { get; set; }
        public string Description      { get; set; }
        public List<string> AttachedFilePaths { get; set; } = new List<string>();
        public DateTime DateReported   { get; set; } = DateTime.Now;
        public string Status           { get; set; } = "Pending";
    }

    /// <summary>
    /// In-memory repository backed by a <see cref="List{IssueReport}"/>.
    /// Provides CRUD helpers and tracks citizen engagement points.
    /// </summary>
    public static class IssueReportRepository
    {
        // ── Core data structure ────────────────────────────────────────────
        private static readonly List<IssueReport> _reports = new List<IssueReport>();
        private static int _nextId = 1;

        // ── Engagement ────────────────────────────────────────────────────
        public static int TotalPoints { get; private set; } = 0;

        // ── Write operations ──────────────────────────────────────────────

        /// <summary>Adds a new report to the list and awards 10 engagement points.</summary>
        public static void AddReport(IssueReport report)
        {
            report.Id = _nextId++;
            _reports.Add(report);
            TotalPoints += 10;
        }

        // ── Read operations (demonstrating List data-structure usage) ──────

        /// <summary>Returns a read-only view of all stored reports.</summary>
        public static IReadOnlyList<IssueReport> GetAll()
        {
            return _reports.AsReadOnly();
        }

        /// <summary>Returns the total number of reports in the list.</summary>
        public static int GetCount()
        {
            return _reports.Count;
        }

        /// <summary>Finds a single report by its ID; returns null if not found.</summary>
        public static IssueReport FindById(int id)
        {
            return _reports.Find(r => r.Id == id);
        }

        /// <summary>Returns all reports that match the specified category.</summary>
        public static List<IssueReport> GetByCategory(string category)
        {
            return _reports.FindAll(r =>
                string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Returns all reports sorted by date descending (newest first).</summary>
        public static List<IssueReport> GetSortedByDate()
        {
            var sorted = new List<IssueReport>(_reports);
            sorted.Sort((a, b) => b.DateReported.CompareTo(a.DateReported));
            return sorted;
        }

        // ── Engagement helpers ────────────────────────────────────────────

        public static string GetUserLevel()
        {
            if (TotalPoints >= 100) return "Community Champion";
            if (TotalPoints >= 50)  return "Active Resident";
            if (TotalPoints >= 20)  return "Engaged Citizen";
            return "New Resident";
        }

        public static int LevelProgress()
        {
            return Math.Min(TotalPoints, 100);
        }
    }
}

