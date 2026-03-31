using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    /// <summary>
    /// Represents the stage or purpose of the visit in the ministry.
    /// </summary>
    public enum VisitType
    {
        /// <summary>
        /// First contact with the individual.
        /// </summary>
        [Display(Name = "Initial Call")]
        InitialCall,

        /// <summary>
        /// Follow-up visit after the initial call.
        /// </summary>
        [Display(Name = "Return Visit")]
        ReturnVisit,

        /// <summary>
        /// A structured Bible study session.
        /// </summary>
        [Display(Name = "Bible Study")]
        BibleStudy
    }
}