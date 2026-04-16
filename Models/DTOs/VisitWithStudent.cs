// ---------------------------------------------------------------------------------------------------------------------
// VisitWithStudent.cs
//
// PURPOSE
// - Lightweight DTO combining Visit and basic Student info for UI display.
//
// DESIGN RULES
// - Not stored in database.
// - Contains only fields needed for lists, calendar, and previews.
// - Keep minimal to avoid over-fetching or duplication.
//
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models.DTOs
{
    public class VisitWithStudent
    {
        public int VisitId { get; set; }

        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public DateTime ScheduledDateTime { get; set; }

        public VisitStatus Status { get; set; }

        public string NotesPreview { get; set; } = string.Empty;
    }
}