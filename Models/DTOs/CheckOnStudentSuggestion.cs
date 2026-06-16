using System;

namespace MinistryTracker.Models.DTOs
{
    public class CheckOnStudentSuggestion
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public DateTime? LastVisitDate { get; set; }

        public string LastVisitDisplay =>
            LastVisitDate is DateTime d
                ? $"Last visit: {d:MMM d, yyyy}"
                : "No visit recorded";

        public string ReasonDisplay =>
            LastVisitDate is DateTime d
                ? $"{(DateTime.Today - d.Date).Days} days since last visit"
                : "No future visit scheduled";
    }
}
