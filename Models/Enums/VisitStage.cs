using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    /// <summary>
    /// Represents the stage of the relationship at the time of the visit.
    /// This answers: "What kind of contact is this in the overall progression?"
    /// </summary>
    public enum VisitStage
    {
        /// <summary>
        /// First contact with the individual.
        /// </summary>
        [Display(Name = "Initial Contact")]
        InitialContact,

        /// <summary>
        /// Follow-up contact after the initial contact.
        /// This is the most common stage for ongoing visits.
        /// </summary>
        [Display(Name = "Return Visit")]
        ReturnVisit,

        /// <summary>
        /// A regular Bible study session.
        /// </summary>
        [Display(Name = "Bible Study")]
        BibleStudy
    }
}