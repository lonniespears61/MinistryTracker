// ---------------------------------------------------------------------------------------------------------------------
// Student.cs
// Domain model representing an individual being contacted in the ministry.
//
// DESIGN NOTES
// - Student holds identity, contact info, overall relationship state,
//   and the person's primary known location.
// - "PrimaryAddress" may be a home address or another usual meeting location.
// - "IsHomeAddress" tells us whether the primary address is actually the person's home.
// - Visit records hold interaction history (what happened, when, where).
// - This model uses sqlite-net-pcl attributes.
// ---------------------------------------------------------------------------------------------------------------------

using SQLite;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models
{
    [Table("Students")]
    public class Student
    {
        /// <summary>
        /// Primary key for this student record.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int StudentId { get; set; }

        /// <summary>
        /// Full name of the individual.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// How the first contact with this individual was made.
        /// This is a historical value and does not change.
        /// </summary>
        public InitialContactType InitialContactType { get; set; }

        /// <summary>
        /// Date of first contact.
        /// </summary>
        public DateTime FirstContactDate { get; set; }

        /// <summary>
        /// Primary phone number or contact number for the individual.
        /// Used for calls or texting.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Main known address or usual location associated with this person.
        /// In most cases this will also be the default starting point for new visit locations.
        /// </summary>
        public string? PrimaryAddress { get; set; }

        /// <summary>
        /// True when the primary address is the person's home address.
        /// False when it is another meeting location.
        /// </summary>
        public bool IsHomeAddress { get; set; } = false;

        /// <summary>
        /// Optional latitude for the primary location.
        /// </summary>
        public double? PrimaryLatitude { get; set; }

        /// <summary>
        /// Optional longitude for the primary location.
        /// </summary>
        public double? PrimaryLongitude { get; set; }

        /// <summary>
        /// Tracks whether location capture / geocoding has succeeded for the primary location.
        /// </summary>
        public GeocodeStatus PrimaryGeocodeStatus { get; set; } = GeocodeStatus.None;

        /// <summary>
        /// Preferred language for communication or study.
        /// </summary>
        public string? PreferredLanguage { get; set; }

        /// <summary>
        /// Preferred or most commonly used method of communication.
        /// This is a default/memory aid and may change over time.
        /// </summary>
        public ContactMethod? DefaultContactMethod { get; set; }

        /// <summary>
        /// Current ministry standing / progression of the individual.
        /// (Promising, Interested, Return Visit, Study)
        /// </summary>
        public InterestLevel InterestLevel { get; set; }

        /// <summary>
        /// Indicates this individual should not be contacted again.
        /// Used as a hard filter in most queries.
        /// </summary>
        public bool IsDoNotCall { get; set; } = false;

        /// <summary>
        /// General status of the student relationship.
        /// Does not replace Do Not Call.
        /// </summary>
        public StudentStatus Status { get; set; } = StudentStatus.Active;

        /// <summary>
        /// Optional demographic field.
        /// Usually not required on first entry.
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Optional demographic field.
        /// Usually not required on first entry.
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// Optional link to a household for shared address and region.
        /// </summary>
        public int? HouseholdId { get; set; }

        /// <summary>
        /// Navigation property for convenience (not stored).
        /// </summary>
        [Ignore]
        public Household? Household { get; set; }

        /// <summary>
        /// General notes about the individual (background, needs, personality, etc.).
        /// </summary>
        public string? Notes { get; set; } = string.Empty;

        /// <summary>
        /// Soft delete flag. Record remains in DB but is hidden from normal use.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}