// ---------------------------------------------------------------------------------------------------------------------
// StudentStatus.cs
//
// PURPOSE
// - Represents the current relationship state of a Return Visit.
//
// DESIGN RULES
// - Status describes the current state of the relationship, not activity totals.
// - Completed and Discontinued are removed from normal working flow.
// - Paused remains valid and may resurface later.
// - Do Not Call does not exist in this app.
//
// ---------------------------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    public enum StudentStatus
    {
        [Display(Name = "Active")]
        Active,

        [Display(Name = "Paused")]
        Paused,

        [Display(Name = "Discontinued")]
        Discontinued,

        [Display(Name = "Completed")]
        Completed
    }
}