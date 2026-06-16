using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models
{
    public class ServiceDaySettings
    {
        public ServicePeriod Sunday { get; set; } = ServicePeriod.None;
        public ServicePeriod Monday { get; set; } = ServicePeriod.None;
        public ServicePeriod Tuesday { get; set; } = ServicePeriod.None;
        public ServicePeriod Wednesday { get; set; } = ServicePeriod.None;
        public ServicePeriod Thursday { get; set; } = ServicePeriod.None;
        public ServicePeriod Friday { get; set; } = ServicePeriod.None;
        public ServicePeriod Saturday { get; set; } = ServicePeriod.None;

        public ServicePeriod GetPeriod(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Sunday => Sunday,
                DayOfWeek.Monday => Monday,
                DayOfWeek.Tuesday => Tuesday,
                DayOfWeek.Wednesday => Wednesday,
                DayOfWeek.Thursday => Thursday,
                DayOfWeek.Friday => Friday,
                DayOfWeek.Saturday => Saturday,
                _ => ServicePeriod.None
            };
        }

        public bool HasAnyConfiguredDay()
        {
            return Sunday != ServicePeriod.None ||
                   Monday != ServicePeriod.None ||
                   Tuesday != ServicePeriod.None ||
                   Wednesday != ServicePeriod.None ||
                   Thursday != ServicePeriod.None ||
                   Friday != ServicePeriod.None ||
                   Saturday != ServicePeriod.None;
        }
    }
}