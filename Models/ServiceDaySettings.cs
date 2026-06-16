using MinistryTracker.Models.Enums;

namespace MinistryTracker.Models
{
    public class ServiceDaySettings
    {
        public List<ServiceDayRule> ActiveDays { get; set; } = new();

        public ServicePeriod Sunday { get; set; } = ServicePeriod.None;
        public ServicePeriod Monday { get; set; } = ServicePeriod.None;
        public ServicePeriod Tuesday { get; set; } = ServicePeriod.None;
        public ServicePeriod Wednesday { get; set; } = ServicePeriod.None;
        public ServicePeriod Thursday { get; set; } = ServicePeriod.None;
        public ServicePeriod Friday { get; set; } = ServicePeriod.None;
        public ServicePeriod Saturday { get; set; } = ServicePeriod.None;

        public ServicePeriod GetPeriod(DayOfWeek day)
        {
            return GetPeriods(day).FirstOrDefault();
        }

        public IReadOnlyList<ServicePeriod> GetPeriods(DayOfWeek day)
        {
            var periods = ActiveDays
                .Where(x => x.Day == day && x.Period != ServicePeriod.None)
                .Select(x => x.Period)
                .Distinct()
                .ToList();

            if (periods.Count > 0)
                return periods;

            var legacyPeriod = day switch
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

            return legacyPeriod == ServicePeriod.None
                ? Array.Empty<ServicePeriod>()
                : new[] { legacyPeriod };
        }

        public bool HasAnyConfiguredDay()
        {
            return ActiveDays.Any(x => x.Period != ServicePeriod.None) ||
                   Sunday != ServicePeriod.None ||
                   Monday != ServicePeriod.None ||
                   Tuesday != ServicePeriod.None ||
                   Wednesday != ServicePeriod.None ||
                   Thursday != ServicePeriod.None ||
                   Friday != ServicePeriod.None ||
                   Saturday != ServicePeriod.None;
        }
    }
}
