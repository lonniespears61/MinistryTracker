using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    public enum VisitType
    {
        [Display(Name = "Initial Call")]
        InitialCall,

        [Display(Name = "Return Visit")]
        ReturnVisit,

        [Display(Name = "Bible Study")]
        BibleStudy,

        [Display(Name = "Letter Writing")]
        LetterWriting,

        [Display(Name = "Cart/SPMW")]
        CartWitnessing,

        [Display(Name = "Informal Witnessing")]
        InformalWitnessing,

        [Display(Name = "Phone Call")]
        PhoneCall,

        [Display(Name = "Video Call")]
        VideoCall
    }
}
