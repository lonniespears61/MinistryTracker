using SQLite;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models
{
    /// <summary>
    /// Represents a Bible student with optional household linkage and contact metadata.
    /// </summary>
    [Table("Students")]
    public class Student
    {
        [PrimaryKey, AutoIncrement]
        public int StudentId { get; set; }

        /// <summary>
        /// Student's full name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of initial contact (e.g., House to House, Phone, etc.)
        /// </summary>
        public InitialCallType CallType { get; set; }

        /// <summary>
        /// When the first contact occurred.
        /// </summary>
        public DateTime FirstContactDate { get; set; }

        /// <summary>
        /// Physical address where the study is conducted.
        /// </summary>
        public string? StudyAddress { get; set; }

        /// <summary>
        /// Geolocation data for "near me" or mapping features.
        /// </summary>
        public double? StudyLatitude { get; set; }
        public double? StudyLongitude { get; set; }
        // Geocoding bookkeeping (helps us avoid retrying too often)
        public GeocodeStatus GeocodeStatus { get; set; } = GeocodeStatus.None;

        // Last time we attempted forward/reverse geocoding (UTC)
        public DateTime? LastGeocodeAttemptUtc { get; set; }

        public Gender? Gender { get; set; }
        public int? Age { get; set; }

        /// <summary>
        /// Preferred spoken or written language of the student.
        /// </summary>
        public string? PreferredLanguage { get; set; }

        public ContactMethod? ContactMethod { get; set; }

        /// <summary>
        /// Level of spiritual interest at time of entry.
        /// </summary>
        public InterestLevel InterestLevel { get; set; }

        public StudyLocationType StudyLocationType { get; set; } = StudyLocationType.Home;

        /// <summary>
        /// ID of the household this student is linked to.
        /// </summary>
        public int? HouseholdId { get; set; }

        /// <summary>
        /// Region, territory, or local area reference.
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Notes about background, needs, or spiritual progress.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Indicates whether this student is actively being visited.
        /// </summary>
        public StudentStatus Status { get; set; } = StudentStatus.Active;

        /// <summary>
        /// If true, this student is logically deleted (but still in the DB).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// [Ignored] In-memory reference to a related household object (not stored in DB).
        /// </summary>
        [Ignore]
        public Household? Household { get; set; }

    }
}
