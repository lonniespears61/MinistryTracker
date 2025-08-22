// ---------------------------------------------------------------------------------------------------------------------
// DataService.Visits.cs
// Query helpers for Visit-related data. These keep raw DB logic out of your ViewModels.
// No direct SQL is required here; sqlite-net's LINQ-like API is sufficient.
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;
using System.Globalization;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Visits scheduled between [start, end). Canceled optionally excluded.
        /// </summary>
        public Task<List<Visit>> GetVisitsBetweenAsync(DateTime startInclusive, DateTime endExclusive, bool includeCanceled = false)
        {
            var query = Db.Table<Visit>()
                          .Where(v => v.ScheduledDateTime >= startInclusive &&
                                      v.ScheduledDateTime < endExclusive);

            if (!includeCanceled)
                query = query.Where(v => v.Status != VisitStatus.Canceled);

            return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
        }

        /// <summary>
        /// Today’s visits: midnight → midnight, inclusive start, exclusive end.
        /// </summary>
        public Task<List<Visit>> GetVisitsTodayAsync()
        {
            var start = DateTime.Today;
            var end = start.AddDays(1);
            return Db.Table<Visit>()
                     .Where(v => v.ScheduledDateTime >= start && v.ScheduledDateTime < end)
                     .OrderBy(v => v.ScheduledDateTime)
                     .ToListAsync();
        }

        /// <summary>
        /// Upcoming scheduled visits within N days from now.
        /// </summary>
        public Task<List<Visit>> GetUpcomingVisitsAsync(int days = 7)
        {
            var start = DateTime.Now;
            var end = start.AddDays(days);
            return Db.Table<Visit>()
                     .Where(v => v.Status == VisitStatus.Scheduled &&
                                 v.ScheduledDateTime >= start &&
                                 v.ScheduledDateTime <= end)
                     .OrderBy(v => v.ScheduledDateTime)
                     .ToListAsync();
        }

        /// <summary>
        /// All visits in the current week per device culture (Sunday/Monday start respected).
        /// </summary>
        public async Task<List<Visit>> GetVisitsThisWeekAsync(bool includeCanceled = true)
        {
            var (start, end) = GetThisWeekRange();
            var query = Db.Table<Visit>()
                          .Where(v => v.ScheduledDateTime >= start && v.ScheduledDateTime < end);

            if (!includeCanceled)
                query = query.Where(v => v.Status != VisitStatus.Canceled);

            return await query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
        }

        /// <summary>
        /// Helper: compute the current week's [start, end) by culture.
        /// </summary>
        private static (DateTime start, DateTime end) GetThisWeekRange()
        {
            var culture = CultureInfo.CurrentCulture;
            var first = culture.DateTimeFormat.FirstDayOfWeek; // Sun in US, Mon elsewhere
            var today = DateTime.Today;

            int diff = (7 + (today.DayOfWeek - first)) % 7;
            var start = today.AddDays(-diff);
            var end = start.AddDays(7);
            return (start, end);
        }

        // ------------------------- Using DTOs ---------------------------------
        // WHY DTOs?
        // - Your database entities are persistence-optimized (flat, simple, FK ids).
        // - Your UI often needs *composed* views: Visit + Student.Name, Address, etc.
        // - A DTO (Data Transfer Object) is a lightweight shape tailored for the view.
        //   It prevents polluting your entity with UI-only properties and avoids
        //   tight coupling between data and presentation layers.

        /// <summary>
        /// Returns visits for the current week, *joined in memory* with Student
        /// data to produce a UI-friendly DTO. This avoids complex SQL and keeps
        /// the entity models simple.
        /// </summary>
        public async Task<List<VisitWithStudent>> GetVisitsWithStudentsThisWeekAsync(bool includeCanceled = true)
        {
            var visits = await GetVisitsThisWeekAsync(includeCanceled);

            // Small data set assumption: load all relevant students and join in memory.
            // If you outgrow this, fetch only the required student IDs.
            var allStudents = await Db.Table<Student>().Where(s => !s.IsDeleted).ToListAsync();
            var byId = allStudents.ToDictionary(s => s.StudentId);

            var result = new List<VisitWithStudent>(visits.Count);
            foreach (var v in visits)
            {
                if (byId.TryGetValue(v.StudentId, out var stu))
                {
                    result.Add(new VisitWithStudent
                    {
                        VisitId = v.Id,
                        StudentId = v.StudentId,
                        StudentName = stu.Name ?? "(Unnamed)",
                        ScheduledDateTime = v.ScheduledDateTime,
                        Status = v.Status,
                        VisitType = v.VisitType,
                        NotesPreview = string.IsNullOrWhiteSpace(v.Notes) ? "" : v.Notes!.Length > 80 ? v.Notes![..80] + "…" : v.Notes
                    });
                }
            }

            // Sort for pleasant UI consumption (earliest first).
            return result.OrderBy(x => x.ScheduledDateTime).ToList();
        }
    }
}
