// ---------------------------------------------------------------------------------------------------------------------
// Student.cs
// Domain model representing an individual being contacted in the ministry.
//
// DESIGN NOTES
// - Student holds identity, contact info, and overall relationship state.
// - Visit records hold interaction history (what happened, when, where).
// - Household (if used) holds shared address/region data.
// - This model uses sqlite-net-pcl attributes.
//
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
        /// </summary>
        public StudentStatus Status { get; set; } = StudentStatus.Active;

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