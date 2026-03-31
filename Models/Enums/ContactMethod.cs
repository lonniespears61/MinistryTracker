using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    /// <summary>
    /// Represents the primary method currently used to communicate with the individual.
    /// This reflects the usual or default method, not every interaction.
    /// Actual visit/contact method is tracked per Visit.
    /// </summary>
    public enum ContactMethod
    {
        /// <summary>
        /// Communication is primarily conducted via phone calls.
        /// </summary>
        [Display(Name = "Phone")]
        Phone,

        /// <summary>
        /// Communication is primarily conducted via text messaging (SMS).
        /// </summary>
        [Display(Name = "Text")]
        Text,

        /// <summary>
        /// Communication is primarily conducted via WhatsApp messaging.
        /// </summary>
        [Display(Name = "WhatsApp")]
        WhatsApp,

        /// <summary>
        /// Communication is primarily conducted through in-person visits.
        /// </summary>
        [Display(Name = "In Person")]
        InPerson,

        /// <summary>
        /// Communication is primarily conducted via email.
        /// </summary>
        [Display(Name = "Email")]
        Email,

        /// <summary>
        /// Communication is conducted through another method not listed.
        /// </summary>
        [Display(Name = "Other")]
        Other
    }
}