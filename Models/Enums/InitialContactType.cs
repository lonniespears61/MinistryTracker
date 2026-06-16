using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    /// <summary>
    /// Represents how the first contact with the individual was made.
    /// This is a historical value and does not change over time.
    /// It does NOT represent current or future visit methods.
    /// </summary>
    public enum InitialContactType
    {
        /// <summary>
        /// Initial contact was made through house-to-house ministry.
        /// </summary>
        [Display(Name = "House to House")]
        HouseToHouse,

        /// <summary>
        /// Initial contact was made through a public witnessing cart.
        /// </summary>
        [Display(Name = "Cart")]
        Cart,

        /// <summary>
        /// Initial contact was made through informal or special metropolitan witnessing.
        /// </summary>
        [Display(Name = "SMPW")]
        SMPW,

        /// <summary>
        /// Initial contact was made via a phone call.
        /// </summary>
        [Display(Name = "Phone")]
        Phone,

        /// <summary>
        /// Initial contact was made through a letter.
        /// </summary>
        [Display(Name = "Letter")]
        Letter,

        /// <summary>
        /// Initial contact was made through another method not listed.
        /// </summary>
        [Display(Name = "Other")]
        Other
    }
}