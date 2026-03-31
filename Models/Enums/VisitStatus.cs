using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    /// <summary>
    /// Represents the outcome or current state of a single visit attempt.
    /// One Visit record = one attempt.
    /// </summary>
    public enum VisitStatus
    {
        /// <summary>
        /// The visit is planned and has not yet occurred.
        /// </summary>
        [Display(Name = "Scheduled")]
        Scheduled,

        /// <summary>
        /// The visit occurred as planned.
        /// </summary>
        [Display(Name = "Completed")]
        Completed,

        /// <summary>
        /// The visit was canceled and did not occur.
        /// A future visit, if created later, starts a new chain.
        /// </summary>
        [Display(Name = "Canceled")]
        Canceled,

        /// <summary>
        /// The visit was moved to a different time or place.
        /// A new Visit record should be created and linked.
        /// </summary>
        [Display(Name = "Rescheduled")]
        Rescheduled,

        /// <summary>
        /// The visit did not occur because the individual was unavailable
        /// or did not keep the appointment.
        /// </summary>
        [Display(Name = "No Show")]
        NoShow
    }
}