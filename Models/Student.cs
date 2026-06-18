// ---------------------------------------------------------------------------------------------------------------------
// Student.cs
// Domain model representing a Return Visit (person + relationship).
//
// DESIGN INTENT
// - This model represents WHO the person is and HOW we can reach them again.
// - PrimaryAddress = best known reliable physical place to reach them.
// - IsHomeAddress = indicates whether that address is their actual home.
// - Visit records handle all interaction history and per-visit locations.
// - This model remains language-neutral (no UI text stored here).
// ---------------------------------------------------------------------------------------------------------------------

using SQLite;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models
{
    [Table("Students")]
    public class Student
    {
        /// <summary>
        /// Primary key for this record.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int StudentId { get; set; }

        /// <summary>
        /// Full name of the individual.
        /// Required for all records.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// How the first contact was made.
        /// Historical value (does not change).
        /// </summary>
        public InitialContactType InitialContactType { get; set; }

        /// <summary>
        /// Date of first contact.
        /// </summary>
        public DateTime FirstContactDate { get; set; }

        /// <summary>
        /// Primary phone number (call/text).
        /// Optional but may serve as an anchor.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Email address if available.
        /// Optional anchor.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Best known reliable physical location to reach this person.
        /// May be home, work, or other consistent place.
        /// Used for map and in-person planning.
        /// </summary>
        public string? PrimaryAddress { get; set; }

        /// <summary>
        /// True if PrimaryAddress is the person's actual home.
        /// </summary>
        public bool IsHomeAddress { get; set; } = false;

        /// <summary>
        /// Indicates the general type of location for planning context.
        /// (Home, Work, Public)
        /// </summary>
        public LocationContext LocationContext { get; set; } = LocationContext.Home;

        /// <summary>
        /// Latitude for PrimaryAddress (if known).
        /// </summary>
        public double? PrimaryLatitude { get; set; }

        /// <summary>
        /// Longitude for PrimaryAddress (if known).
        /// </summary>
        public double? PrimaryLongitude { get; set; }

        /// <summary>
        /// Tracks whether geocoding has succeeded.
        /// </summary>
        public GeocodeStatus PrimaryGeocodeStatus { get; set; } = GeocodeStatus.None;

        /// <summary>
        /// Preferred language for communication or study.
        /// Stored as user-entered value (not auto-translated).
        /// </summary>
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// Default / most common method of contact.
        /// Used as a convenience when scheduling visits.
        /// </summary>
        public ContactMethod? PreferredContactMethod { get; set; }

        /// <summary>
        /// Indicates level of development (interest → study).
        /// Lightweight guidance, not strict workflow.
        /// </summary>
        public InterestLevel InterestLevel { get; set; }

        /// <summary>
        /// Current relationship status.
        /// Controls visibility in dashboard/map logic.
        /// </summary>
        public StudentStatus Status { get; set; } = StudentStatus.Active;

        /// <summary>
        /// Optional demographic field.
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Optional demographic field.
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// General notes about the individual.
        /// Used for background, personality, and long-term context.
        /// </summary>
        public string? Notes { get; set; } = string.Empty;

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
