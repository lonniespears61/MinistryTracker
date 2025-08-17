using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;
using SQLite; // for Table<T>(), ToListAsync(), etc.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using MinistryTracker.Models.DTOs;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        // Today's visits (midnight → midnight)
        public Task<List<Visit>> GetVisitsTodayAsync()
        {
            var start = DateTime.Today;
            var end = start.AddDays(1);
            return Db.Table<Visit>()
                     .Where(v => v.ScheduledDateTime >= start &&
                                 v.ScheduledDateTime < end)
                     .OrderBy(v => v.ScheduledDateTime)
                     .ToListAsync();
        }

        // Upcoming scheduled visits within N days
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

        // Arbitrary window; optionally exclude canceled
        public Task<List<Visit>> GetVisitsBetweenAsync(DateTime startInclusive, DateTime endExclusive, bool includeCanceled = false)
        {
            var query = Db.Table<Visit>()
                          .Where(v => v.ScheduledDateTime >= startInclusive &&
                                      v.ScheduledDateTime < endExclusive);

            if (!includeCanceled)
                query = query.Where(v => v.Status != VisitStatus.Canceled);

            return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
        }

        // Upsert convenience
        public Task<int> AddOrUpdateVisitAsync(Visit visit) =>
            visit.Id > 0 ? Db.UpdateAsync(visit) : Db.InsertAsync(visit);

        // Mark completed + optional note append
        public async Task<bool> CompleteVisitAsync(int visitId, string? noteAppend = null)
        {
            var v = await Db.FindAsync<Visit>(visitId);
            if (v is null) return false;

            v.Status = VisitStatus.Completed;
            if (!string.IsNullOrWhiteSpace(noteAppend))
                v.Notes = string.IsNullOrEmpty(v.Notes) ? noteAppend : $"{v.Notes}\n{noteAppend}";

            return await Db.UpdateAsync(v) == 1;
        }

        // Cancel with optional reason
        public async Task<bool> CancelVisitAsync(int visitId, string? reason = null)
        {
            var v = await Db.FindAsync<Visit>(visitId);
            if (v is null) return false;

            v.Status = VisitStatus.Canceled;
            v.CancellationReason = reason;

            return await Db.UpdateAsync(v) == 1;
        }

        // Get week window using device culture (Sunday/Monday start respected)
        private static (DateTime start, DateTime end) GetThisWeekRange()
        {
            var culture = CultureInfo.CurrentCulture;
            var first = culture.DateTimeFormat.FirstDayOfWeek; // Sun in US, Mon in many regions
            var today = DateTime.Today;

            int diff = (7 + (today.DayOfWeek - first)) % 7;
            var start = today.AddDays(-diff);
            var end = start.AddDays(7); // [start, end)
            return (start, end);
        }

        // All visits in this week (optionally exclude canceled)
        public async Task<List<Visit>> GetVisitsThisWeekAsync(bool includeCanceled = true)
        {
            var (start, end) = GetThisWeekRange();
            var query = Db.Table<Visit>()
                          .Where(v => v.ScheduledDateTime >= start && v.ScheduledDateTime < end);

            if (!includeCanceled)
                query = query.Where(v => v.Status != VisitStatus.Canceled);

            return await query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
        }

        // This week, but joined with Student for UI display
        public async Task<List<VisitWithStudent>> GetVisitsWithStudentsThisWeekAsync(bool includeCanceled = true)
        {
            var visits = await GetVisitsThisWeekAsync(includeCanceled);

            // Small data size; simplest path: load students and join in memory
            var allStudents = await Db.Table<Student>().Where(s => !s.IsDeleted).ToListAsync();
            var byId = allStudents.ToDictionary(s => s.StudentId);

            var result = new List<VisitWithStudent>(visits.Count);
            foreach (var v in visits)
            {
                if (byId.TryGetValue(v.StudentId, out var stu))
                {
                    result.Add(new VisitWithStudent { Visit = v, Student = stu });
                }
            }

            // sort by date asc (earliest first)
            return result.OrderBy(x => x.Visit.ScheduledDateTime).ToList();
        }
    }
}
