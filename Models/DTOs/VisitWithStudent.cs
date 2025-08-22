// ---------------------------------------------------------------------------------------------------------------------
// VisitWithStudent.cs (DTO)
// A view-optimized "shape" for list/detail UI that needs Visit data plus a few Student fields.
// We DO NOT store this in the database; it's purely for transport/binding in the UI layer.
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models.DTOs
{
    public class VisitWithStudent
    {
        // Identity: still handy for commands like "open visit" or "mark complete"
        public int VisitId { get; set; }

        // The foreign key for linking back to Student if needed
        public int StudentId { get; set; }

        // UI-friendly student info (no need to drag the full Student object)
        public string StudentName { get; set; } = string.Empty;

        // The scheduled date/time for this visit (local time for display)
        public DateTime ScheduledDateTime { get; set; }

        // Enum fields are great in the ViewModel/UI for switch/case or converters
        public VisitStatus Status { get; set; }
        public VisitType VisitType { get; set; }

        // Optional small preview for list cards; full notes can be loaded on demand
        public string NotesPreview { get; set; } = string.Empty;
    }
}
