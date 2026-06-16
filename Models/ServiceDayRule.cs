using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models
{
    public class ServiceDayRule
    {
        public DayOfWeek Day { get; set; }
        public ServicePeriod Period { get; set; }
    }
}
