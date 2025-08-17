using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    public enum VisitStatus
    {
        Scheduled,
        Completed,
        Canceled,
        Cancelled = Canceled, // <-- alias, keeps both spellings valid
        NoShow
    }
}