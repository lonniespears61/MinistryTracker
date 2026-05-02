// ---------------------------------------------------------------------------------------------------------------------
// DataService.Visits.cs
// Visit queries and UI-friendly DTO projections.
// Uses EnsureInitThen(...) so initialization is consistent across partials and supports CancellationToken.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Utilities;

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

        public Task<int> AddVisitAsync(Visit visit, CancellationToken ct = default)
        {
            if (visit is null)
                throw new ArgumentNullException(nameof(visit));

            if (visit.StudentId <= 0)
                throw new InvalidOperationException("Visit.StudentId must be set before inserting a visit.");

            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.InsertAsync(visit);
            }, ct);
        }

        public Task<int> UpdateVisitAsync(Visit visit, CancellationToken ct = default)
        {
            if (visit is null)
                throw new ArgumentNullException(nameof(visit));

            if (visit.Id <= 0)
                throw new InvalidOperationException("Visit.Id must be set before updating a visit.");

            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.UpdateAsync(visit);
            }, ct);
        }

        /// <summary>
        /// Get a visit by primary key.
        /// Returns null if not found.
        /// </summary>
        public Task<Visit?> GetVisitByIdAsync(int visitId, CancellationToken ct = default)
        {
            if (visitId <= 0)
                return Task.FromResult<Visit?>(null);

            return EnsureInitThen<Visit?>(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var result = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                return result;
            }, ct);
        }

        public Task<int> DeleteVisitAsync(int visitId, CancellationToken ct = default)
        {
            if (visitId <= 0)
                return Task.FromResult(0);

            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.DeleteAsync<Visit>(visitId);
            }, ct);
        }

        public Task<List<Visit>> GetVisitsTodayAsync(CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var start = DateTime.Today;
                var end = start.AddDays(1);

                return Db.Table<Visit>()
                         .Where(v => v.ScheduledDateTime >= start &&
                                     v.ScheduledDateTime < end)
                         .OrderBy(v => v.ScheduledDateTime)
                         .ToListAsync();
            }, ct);

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

        public Task<List<Visit>> GetVisitsThisWeekAsync(bool includeCanceled = true, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var (start, end) = DateRanges.GetThisWeekRange();

                var query = Db.Table<Visit>()
                              .Where(v => v.ScheduledDateTime >= start &&
                                          v.ScheduledDateTime < end);

                if (!includeCanceled)
                    query = query.Where(v => v.Status != VisitStatus.Canceled);

                return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
            }, ct);

        public Task CancelVisitAsync(int visitId, string? reason = null, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                Visit? visit = await Db.Table<Visit>()
                                       .Where(v => v.Id == visitId)
                                       .FirstOrDefaultAsync()
                                       .ConfigureAwait(false);

                if (visit is null)
                    return;

                visit.Status = VisitStatus.Canceled;

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    visit.Notes = string.IsNullOrWhiteSpace(visit.Notes)
                        ? $"[Canceled] {reason}"
                        : $"{visit.Notes}\n\n[Canceled] {reason}";
                }

                await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);

        /// <summary>
        /// Next future scheduled visit for a student.
        /// Returns null if none exists.
        /// </summary>
        public Task<Visit?> GetNextFutureVisitForStudentAsync(int studentId, CancellationToken ct = default)
            => EnsureInitThen<Visit?>(async () =>
            {
                var now = DateTime.Now;

                var result = await Db.Table<Visit>()
                                     .Where(v => v.StudentId == studentId &&
                                                 v.Status == VisitStatus.Scheduled &&
                                                 v.ScheduledDateTime >= now)
                                     .OrderBy(v => v.ScheduledDateTime)
                                     .FirstOrDefaultAsync()
                                     .ConfigureAwait(false);

                return result;
            }, ct);
    }
}