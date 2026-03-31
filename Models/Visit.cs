// ---------------------------------------------------------------------------------------------------------------------
// Visit.cs
// Domain model for a single planned or recorded ministry contact attempt in the MinistryTracker app.
//
// DESIGN NOTES
// - One Visit record = one attempt.
// - If a visit is rescheduled, this record is marked Rescheduled and a NEW Visit record is created.
// - If a visit is canceled and a later appointment is made, that later appointment starts a NEW chain.
// - Visit-level location is important because a meeting place may differ from a student's home address,
//   may only be known by geolocation, and may need to be handed off to another publisher.
// - This model uses sqlite-net-pcl attributes, not Entity Framework.
//
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
        /// Foreign key to the Student this visit is associated with.
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// Navigation property for convenience in app code.
        /// Not stored by sqlite-net.
        /// </summary>
        [Ignore]
        public Student? Student { get; set; }

        /// <summary>
        /// The ministry stage represented by this contact attempt
        /// (for example: Initial Contact, Return Visit, Bible Study).
        /// </summary>
        public VisitStage Stage { get; set; }

        /// <summary>
        /// How this interaction is or was conducted
        /// (for example: InPerson, Text, Phone, VideoCall, Cart, Informal).
        /// </summary>
        public ContactMethod Method { get; set; }

        /// <summary>
        /// When this visit is scheduled to occur.
        /// For completed visits, this remains the originally scheduled date/time.
        /// </summary>
        public DateTime ScheduledDateTime { get; set; }

        /// <summary>
        /// Current status of this single visit attempt.
        /// </summary>
        public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

        /// <summary>
        /// When the visit actually occurred, if completed.
        /// </summary>
        public DateTime? CompletedDateTime { get; set; }

        /// <summary>
        /// Optional human-readable meeting address or description for this visit.
        /// This may be a full address, partial description, or simple meeting label.
        /// Examples:
        /// "123 Main St"
        /// "Porch by red barn"
        /// "Meet in field at lunch"
        /// "The Grind"
        /// </summary>
        public string? MeetingAddress { get; set; }

        /// <summary>
        /// Latitude for the meeting location, if captured.
        /// Useful when no reliable address is available or when returning to a pinned rural location.
        /// </summary>
        public double? MeetingLatitude { get; set; }

        /// <summary>
        /// Longitude for the meeting location, if captured.
        /// Useful when no reliable address is available or when returning to a pinned rural location.
        /// </summary>
        public double? MeetingLongitude { get; set; }

        /// <summary>
        /// If this visit was created because another visit was rescheduled,
        /// this links back to the prior visit in that same chain.
        /// This should only be used for true reschedules, not for a brand-new appointment
        /// made after a cancellation.
        /// </summary>
        public int? RescheduledFromVisitId { get; set; }

        /// <summary>
        /// Free-form notes used as a memory aid.
        /// Examples:
        /// what was discussed,
        /// why a change happened,
        /// what question was asked,
        /// observations that may help with future visits,
        /// or details needed for a handoff to another publisher.
        /// </summary>
        public string? Notes { get; set; } = string.Empty;
    }
}