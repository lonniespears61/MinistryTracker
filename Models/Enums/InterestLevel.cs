using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    /// <summary>
    /// Represents the current ministry standing of the individual.
    /// This reflects progression from initial promise to an established study.
    /// Used as a practical memory aid for tracking development over time.
    /// </summary>
    public enum InterestLevel
    {
        /// <summary>
        /// Shows initial promise and may be worth follow-up.
        /// </summary>
        [Display(Name = "Promising")]
        Promising,

        /// <summary>
        /// Shows clear interest and willingness to continue communication.
        /// </summary>
        [Display(Name = "Interested")]
        Interested,

        /// <summary>
        /// Has become a return visit with ongoing contact.
        /// </summary>
        [Display(Name = "Return Visit")]
        ReturnVisit,

        /// <summary>
        /// Has progressed to a regular Bible study.
        /// </summary>
        [Display(Name = "Study")]
        Study
    }
}