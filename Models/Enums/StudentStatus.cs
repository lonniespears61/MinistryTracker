using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    public enum StudentStatus
    {
        [Display(Name = "Active")]
        Active,

        [Display(Name = "Paused")]
        Paused,

        [Display(Name = "Not Interested")]
        NotInterested,

        [Display(Name = "Do Not Call")]
        DoNotCall,

        [Display(Name = "Discontinued")]
        Discontinued
    }
}