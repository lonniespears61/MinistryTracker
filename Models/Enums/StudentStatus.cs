using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    /// <summary>
    /// Represents the current ministry disposition of the student.
    /// Does NOT include Do Not Call, which is handled separately by IsDoNotCall.
    /// </summary>
    public enum StudentStatus
    {
        /// <summary>
        /// Actively being visited and showing interest.
        /// </summary>
        [Display(Name = "Active")]
        Active,

        /// <summary>
        /// Temporarily not being visited due to schedule, health, or other circumstances.
        /// May resume in the future.
        /// </summary>
        [Display(Name = "Paused")]
        Paused,

        /// <summary>
        /// Previously showed interest but is no longer interested in continuing.
        /// </summary>
        [Display(Name = "No Longer Interested")]
        NoLongerInterested,

        /// <summary>
        /// Visits have been intentionally stopped for practical reasons
        /// (moved away, not a good match, etc.), but not a Do Not Call.
        /// </summary>
        [Display(Name = "Discontinued")]
        Discontinued
    }
}