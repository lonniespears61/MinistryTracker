using System.ComponentModel.DataAnnotations;

namespace MinistryTracker.Models.Enums
{
    public enum VisitStatus
    {
        Scheduled,
        Completed,
        Cancelled,
        Rescheduled,
        NeedsReschedule
    }
}