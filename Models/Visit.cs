// ---------------------------------------------------------------------------------------------------------------------
// Visit.cs
// Domain model for one planned or completed follow-up attempt for a Return Visit.
//
// DESIGN INTENT
// - One Visit record = one scheduled attempt.
// - Each Visit stands on its own, even when the same person is contacted repeatedly.
// - If a visit is rescheduled, this record is marked Rescheduled and a NEW Visit record is created.
// - Visit-level location may differ from the student's PrimaryAddress.
// - New visits may default meeting location from the student's primary location,
//   but each Visit remains independently editable.
// - Notes are stored as a single field.
//   On entry, the UI may split this into:
//     1) What happened
//     2) Next Time
//   but the model stores one combined note body to avoid duplication.
// ---------------------------------------------------------------------------------------------------------------------

using SQLite;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models
{
    [Table("Visits")]
    public class Visit
    {
        /// <summary>
        /// Primary key for this visit record.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the Return Visit this attempt belongs to.
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// Navigation property for convenience in app code.
        /// Not stored by sqlite-net.
        /// </summary>
        [Ignore]
        public Student? Student { get; set; }

        /// <summary>
        /// How this visit is planned to happen.
        /// Examples: InPerson, Text, Phone, WhatsApp, Email, Letter.
        /// </summary>
        public ContactMethod Method { get; set; }

        /// <summary>
        /// Scheduled date/time for this visit.
        /// For completed visits, this remains the original planned time.
        /// </summary>
        public DateTime ScheduledDateTime { get; set; }

        /// <summary>
        /// Outcome / current state of this visit attempt.
        /// </summary>
        public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

        /// <summary>
        /// When the visit actually happened, if it was completed successfully.
        /// </summary>
        public DateTime? CompletedDateTime { get; set; }

        /// <summary>
        /// Optional visit-specific meeting address or description.
        /// This may differ from the student's PrimaryAddress.
        /// </summary>
        public string? MeetingAddress { get; set; }

        /// <summary>
        /// Optional latitude for the meeting location.
        /// </summary>
        public double? MeetingLatitude { get; set; }

        /// <summary>
        /// Optional longitude for the meeting location.
        /// </summary>
        public double? MeetingLongitude { get; set; }

        /// <summary>
        /// Links this visit to the prior visit when this one was created by rescheduling.
        /// Used only for true reschedules.
        /// </summary>
        public int? RescheduledFromVisitId { get; set; }

        /// <summary>
        /// Single stored note body for this visit.
        /// The UI may visually separate "What happened" and "Next Time",
        /// but storage stays unified to avoid duplicate writing and duplicate reading.
        /// </summary>
        public string? Notes { get; set; } = string.Empty;

        /// <summary>
        /// When the note content for this visit was last entered or updated.
        /// This is separate from the visit date because notes may be written later.
        /// </summary>
        public DateTime? NotesCreatedDateTime { get; set; }
    }
}