// ---------------------------------------------------------------------------------------------------------------------
// DateRanges.cs — culture-aware date ranges (reusable across apps) — 2026-01-30
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace MinistryTracker.Utilities
{
    public static class DateRanges
    {
        /// <summary>
        /// Compute the current week’s [start, end) by culture (Sun/Mon start respected).
        /// Start is inclusive; end is exclusive.
        /// </summary>
        public static (DateTime start, DateTime end) GetThisWeekRange(CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;

            var first = culture.DateTimeFormat.FirstDayOfWeek; // Sun in US, Mon elsewhere
            var today = DateTime.Today;

            int diff = (7 + (today.DayOfWeek - first)) % 7;
            var start = today.AddDays(-diff);
            var end = start.AddDays(7);

            return (start, end);
        }
    }
}
