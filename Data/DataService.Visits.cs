// ---------------------------------------------------------------------------------------------------------------------
// DataService.Visits.cs
// Visit queries and UI-friendly DTO projections.
// Uses EnsureInitThen(...) so initialization is consistent across partials and supports CancellationToken.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Visits scheduled between [startInclusive, endExclusive). Canceled optionally excluded.
        /// </summary>
        public Task<List<Visit>> GetVisitsBetweenAsync(
            DateTime startInclusive,
            DateTime endExclusive,
            bool includeCanceled = false,
            CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var query = Db.Table<Visit>()
                              .Where(v => v.ScheduledDateTime >= startInclusive &&
                                          v.ScheduledDateTime < endExclusive);

                if (!includeCanceled)
                    query = query.Where(v => v.Status != VisitStatus.Canceled);

                return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
            }, ct);

        /// <summary>
        /// Today’s visits: midnight → midnight (inclusive start, exclusive end).
        /// </summary>
        public Task<List<Visit>> GetVisitsTodayAsync(CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var start = DateTime.Today;
                var end = start.AddDays(1);
                return Db.Table<Visit>()
                         .Where(v => v.ScheduledDateTime >= start && v.ScheduledDateTime < end)
                         .OrderBy(v => v.ScheduledDateTime)
                         .ToListAsync();
            }, ct);

        /// <summary>
        /// Upcoming scheduled visits within N days from now.
        /// </summary>
        public Task<List<Visit>> GetUpcomingVisitsAsync(int days = 7, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var start = DateTime.Now;
                var end = start.AddDays(days);
                return Db.Table<Visit>()
                         .Where(v => v.Status == VisitStatus.Scheduled &&
                                     v.ScheduledDateTime >= start &&
                                     v.ScheduledDateTime <= end)
                         .OrderBy(v => v.ScheduledDateTime)
                         .ToListAsync();
            }, ct);

        /// <summary>
        /// All visits in the current week per device culture (Sunday/Monday start respected).
        /// </summary>
        public Task<List<Visit>> GetVisitsThisWeekAsync(bool includeCanceled = true, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var (start, end) = GetThisWeekRange();
                var query = Db.Table<Visit>()
                              .Where(v => v.ScheduledDateTime >= start && v.ScheduledDateTime < end);

                if (!includeCanceled)
                    query = query.Where(v => v.Status != VisitStatus.Canceled);

                return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
            }, ct);

        /// <summary>
        /// RANGE-BASED DTO helper: visits in [startInclusive, endExclusive) joined with Student into a view-friendly DTO.
        /// Assumes small datasets; for large data consider SQL JOIN or paging.
        /// </summary>
        public Task<List<VisitWithStudent>> GetVisitsWithStudentsInRangeAsync(
            DateTime startInclusive,
            DateTime endExclusive,
            bool includeCanceled = true,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                // 1) visits in range
                var visitsQuery = Db.Table<Visit>()
                                    .Where(v => v.ScheduledDateTime >= startInclusive &&
                                                v.ScheduledDateTime < endExclusive);
                if (!includeCanceled)
                    visitsQuery = visitsQuery.Where(v => v.Status != VisitStatus.Canceled);

                var visits = await visitsQuery.ToListAsync().ConfigureAwait(false);
                if (visits.Count == 0) return new List<VisitWithStudent>();

                // 2) referenced students (not deleted)
                var studentIds = visits.Select(v => v.StudentId).Distinct().ToList();
                var students = await Db.Table<Student>()
                                       .Where(s => studentIds.Contains(s.StudentId) && !s.IsDeleted)
                                       .ToListAsync()
                                       .ConfigureAwait(false);
                var byId = students.ToDictionary(s => s.StudentId);

                // 3) project to DTO
                var result = new List<VisitWithStudent>(visits.Count);
                foreach (var v in visits)
                {
                    ct.ThrowIfCancellationRequested();
                    byId.TryGetValue(v.StudentId, out var stu);

                    result.Add(new VisitWithStudent
                    {
                        VisitId = v.Id, // PK on Visit
                        StudentId = v.StudentId,
                        StudentName = stu?.Name ?? "(Unnamed)",
                        ScheduledDateTime = v.ScheduledDateTime,
                        Status = v.Status,
                        VisitType = v.VisitType,
                        NotesPreview = string.IsNullOrWhiteSpace(v.Notes)
                            ? string.Empty
                            : (v.Notes!.Length > 80 ? v.Notes[..80] + "…" : v.Notes)
                    });
                }

                return result.OrderBy(x => x.ScheduledDateTime).ToList();
            }, ct);

        /// <summary>
        /// WEEK-BASED DTO helper: visits for the current week joined with Student (UI-friendly).
        /// </summary>
        public Task<List<VisitWithStudent>> GetVisitsWithStudentsThisWeekAsync(
            bool includeCanceled = true,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var (start, end) = GetThisWeekRange();

                var visitsQuery = Db.Table<Visit>()
                                    .Where(v => v.ScheduledDateTime >= start &&
                                                v.ScheduledDateTime < end);
                if (!includeCanceled)
                    visitsQuery = visitsQuery.Where(v => v.Status != VisitStatus.Canceled);

                var visits = await visitsQuery.ToListAsync().ConfigureAwait(false);
                if (visits.Count == 0) return new List<VisitWithStudent>();

                var studentIds = visits.Select(v => v.StudentId).Distinct().ToList();
                var students = await Db.Table<Student>()
                                       .Where(s => studentIds.Contains(s.StudentId) && !s.IsDeleted)
                                       .ToListAsync()
                                       .ConfigureAwait(false);
                var byId = students.ToDictionary(s => s.StudentId);

                var result = new List<VisitWithStudent>(visits.Count);
                foreach (var v in visits)
                {
                    ct.ThrowIfCancellationRequested();
                    byId.TryGetValue(v.StudentId, out var stu);

                    result.Add(new VisitWithStudent
                    {
                        VisitId = v.Id,
                        StudentId = v.StudentId,
                        StudentName = stu?.Name ?? "(Unnamed)",
                        ScheduledDateTime = v.ScheduledDateTime,
                        Status = v.Status,
                        VisitType = v.VisitType,
                        NotesPreview = string.IsNullOrWhiteSpace(v.Notes)
                            ? string.Empty
                            : (v.Notes!.Length > 80 ? v.Notes[..80] + "…" : v.Notes)
                    });
                }

                return result.OrderBy(x => x.ScheduledDateTime).ToList();
            }, ct);

        /// <summary>
        /// Compute the current week’s [start, end) by culture (Sun/Mon start respected).
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
    }
}
