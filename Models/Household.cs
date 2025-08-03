using SQLite;


namespace MinistryTracker.Models
{
    [Table("Households")]
    public class Household
    {
        [PrimaryKey, AutoIncrement]
        public int HouseholdId { get; set; }

        /// <summary>
        /// A short name or label to identify the household.
        /// Example: "Rodriguez Family", "Brown Upstairs Apt", "Blue Gate Home"
        /// </summary>
        public string? Label { get; set; } = string.Empty;

        public string? Address { get; set; } // Shared home address

        public string? Region { get; set; }  // Locale, neighborhood, village, etc.

        public string? Notes { get; set; }

        public double? Latitude { get; set; }    // Geolocation for "Near Me"
        public double? Longitude { get; set; }

            }
}
