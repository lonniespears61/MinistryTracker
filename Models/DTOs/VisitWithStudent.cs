// ---------------------------------------------------------------------------------------------------------------------
// VisitWithStudent.cs (DTO)
//
// PURPOSE
// - UI-friendly projection combining Visit + basic Student info
// - Used for lists, calendar, and lightweight display scenarios
// - NOT stored in database
//
// DESIGN RULES
// - Keep it small (only what UI needs)
// - VisitStage replaces old VisitType
// - One Visit = one attempt (status reflects outcome)
//
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models.DTOs
{
    public class VisitWithStudent
    {
        /// <summary>
        /// Visit identity (used for navigation/actions).
        /// </summary>
        public int VisitId { get; set; }

        /// <summary>
        /// Student foreign key.
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// Student display name (no full Student object needed).
        /// </summary>
        public string StudentName { get; set; } = string.Empty;

        /// <summary>
        /// Scheduled date/time for the visit (local time).
        /// </summary>
        public DateTime ScheduledDateTime { get; set; }

        /// <summary>
        /// Current state of the visit (Scheduled, Completed, etc.).
        /// </summary>
        public VisitStatus Status { get; set; }

        /// <summary>
        /// Stage of the interaction (Initial Contact, Return Visit, Bible Study).
        /// </summary>
        public VisitStage Stage { get; set; }

        /// <summary>
        /// Short preview of notes for list display.
        /// </summary>
        public string NotesPreview { get; set; } = string.Empty;
    }
}