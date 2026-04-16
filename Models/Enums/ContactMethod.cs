// ---------------------------------------------------------------------------------------------------------------------
// ContactMethod.cs
//
// PURPOSE
// - Represents how a visit is conducted (method of contact).
//
// DESIGN RULES
// - Used at both Student (preferred method) and Visit (actual method).
// - Must reflect real-world communication methods used in ministry.
// - Keep values simple and recognizable.
//
// ---------------------------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    public enum ContactMethod
    {
        [Display(Name = "Phone")]
        Phone,

        [Display(Name = "Text")]
        Text,

        [Display(Name = "WhatsApp")]
        WhatsApp,

        [Display(Name = "In Person")]
        InPerson,

        [Display(Name = "Email")]
        Email,

        [Display(Name = "Letter")]
        Letter,

        [Display(Name = "Other")]
        Other
    }
}