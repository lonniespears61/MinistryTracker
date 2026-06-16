using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Utilities
{
    /// <summary>
    /// Calculates the default visit date/time based on the user's
    /// Normal Service Days settings.
    /// </summary>
    public static class VisitSchedulingHelper
    {
        private static readonly TimeSpan DefaultFallbackTime = new(10, 0, 0);

        public static DateTime GetDefaultVisitDateTime(
            ServiceDaySettings settings,
            DateTime? selectedDate = null)
        {
            // ------------------------------------------------------------
            // Rule 1:
            // If an explicit date is provided, keep that date.
            // Use that day's configured default time.
            // If that day is not configured, use 10:00 AM.
            // ------------------------------------------------------------
            if (selectedDate.HasValue)
            {
                var date = selectedDate.Value.Date;
                var period = GetFirstPeriod(settings, date.DayOfWeek);
                var time = ToTime(period);

                return date + time;
            }

            var today = DateTime.Today;

            // ------------------------------------------------------------
            // Rule 2:
            // If no days are configured, use the next Saturday at 10:00 AM.
            // ------------------------------------------------------------
            if (!settings.HasAnyConfiguredDay())
            {
                for (int i = 1; i <= 7; i++)
                {
                    var candidate = today.AddDays(i);

                    if (candidate.DayOfWeek == DayOfWeek.Saturday)
                        return candidate + DefaultFallbackTime;
                }
            }

            // ------------------------------------------------------------
            // Rule 3:
            // If today is a configured service day, do NOT use today.
            // Use the same day one week ahead at that day's default time.
            // ------------------------------------------------------------
            var todayPeriod = GetFirstPeriod(settings, today.DayOfWeek);
            if (todayPeriod != ServicePeriod.None)
            {
                var nextWeek = today.AddDays(7);
                return nextWeek + ToTime(todayPeriod);
            }

            // ------------------------------------------------------------
            // Rule 4:
            // Today is not a configured service day.
            // Find the next configured service day after today.
            // ------------------------------------------------------------
            for (int i = 1; i <= 7; i++)
            {
                var candidate = today.AddDays(i);
                var period = GetFirstPeriod(settings, candidate.DayOfWeek);

                if (period != ServicePeriod.None)
                {
                    return candidate + ToTime(period);
                }
            }

            // Should never hit, but fallback anyway.
            return today.AddDays(1) + DefaultFallbackTime;
        }

        /// <summary>
        /// Maps service period to an actual default visit time.
        /// </summary>
        private static TimeSpan ToTime(ServicePeriod period)
        {
            return period switch
            {
                ServicePeriod.Morning => new TimeSpan(10, 0, 0),
                ServicePeriod.Afternoon => new TimeSpan(13, 0, 0),
                ServicePeriod.Evening => new TimeSpan(18, 0, 0),
                _ => DefaultFallbackTime
            };
        }

        private static ServicePeriod GetFirstPeriod(ServiceDaySettings settings, DayOfWeek day)
        {
            return settings.GetPeriods(day)
                .OrderBy(GetPeriodSortOrder)
                .FirstOrDefault();
        }

        private static int GetPeriodSortOrder(ServicePeriod period)
        {
            return period switch
            {
                ServicePeriod.Morning => 0,
                ServicePeriod.Afternoon => 1,
                ServicePeriod.Evening => 2,
                _ => 3
            };
        }
    }
}
