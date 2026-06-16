// ---------------------------------------------------------------------------------------------------------------------
// VisitStatus.cs
//
// PURPOSE
// - Defines the outcome/state of a single visit attempt.
// - One Visit = one attempt.
//
// DESIGN RULES
// - Used to drive lifecycle, dashboard behavior, and follow-up logic.
// - Values must align with real-world outcomes (no generic or ambiguous states).
//
// ---------------------------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    public enum VisitStatus
    {
        [Display(Name = "Scheduled")]
        Scheduled,

        [Display(Name = "Successful")]
        Successful,

        [Display(Name = "Missed")]
        Missed,

        [Display(Name = "Canceled (Me)")]
        CanceledByMe,

        [Display(Name = "Canceled (Them)")]
        CanceledByThem,

        [Display(Name = "Rescheduled")]
        Rescheduled
    }
}