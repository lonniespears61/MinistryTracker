// ---------------------------------------------------------------------------------------------------------------------
// Visit.cs
// Domain model for a scheduled/recorded visit in the MinistryTracker app.
//
// IMPORTANT: This model uses the attributes from **sqlite-net-pcl**, not Entity Framework.
// - sqlite-net maps simple CLR properties (int, string, DateTime, enums) to columns
// - Complex navigation properties (like Student) must be marked [Ignore]
// - Enums are stored as integers automatically
//
// ---------------------------------------------------------------------------------------------------------------------

using SQLite;                          // ✅ sqlite-net attributes (PrimaryKey, AutoIncrement, Table, Ignore)
using MinistryTracker.Models.Enums;    // ✅ VisitType, VisitStatus enums live here

namespace MinistryTracker.Models
{
    // Pin the table name explicitly. This protects you from accidental renames or pluralization drift later.
    [Table("Visit")]
    public class Visit
    {
        // Primary key with auto-increment identity. sqlite-net handles this pattern nicely.
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Foreign key to the Student table by integer ID.
        // Note: sqlite-net does NOT enforce foreign keys or relationships for you.
        //       We keep the FK field and perform any "joins" in memory at the DataService layer.
        public int StudentId { get; set; }

        // Navigation property: useful in app code, but NOT storable by sqlite-net. Must be ignored.
        // We'll populate this manually when assembling view models (see DataService join helpers).
        [Ignore]
        public Student? Student { get; set; }

        // When this visit is/was scheduled to happen.
        // Stored in local time for simplicity; if you need robust TZ handling, consider UTC + conversion at the edges.
        public DateTime ScheduledDateTime { get; set; }

        // Current state of the visit. sqlite-net stores enums as ints automatically.
        public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

        // Free-form notes about what was discussed, follow-ups, etc.
        // Using nullable string with an empty-string default to avoid null checks in UI bindings.
        public string? Notes { get; set; } = string.Empty;

        // If a visit is canceled, we can capture why for later review.
        public string? CancellationReason { get; set; }

        // What kind of visit this is (return visit, study, letter writing, etc.).
        public VisitType VisitType { get; set; } = VisitType.ReturnVisit;
        public string? LocationAddressOverride { get; set; }

        public double? LocationLatitudeOverride { get; set; }

        public double? LocationLongitudeOverride { get; set; }
    }
}
