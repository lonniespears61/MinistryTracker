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

        // ---------------------------------------------------------------------
        // CRUD
        // ---------------------------------------------------------------------

        public Task<int> AddVisitAsync(Visit visit, CancellationToken ct = default)
        {
            if (visit is null)
                throw new ArgumentNullException(nameof(visit));

            if (visit.StudentId <= 0)
                throw new InvalidOperationException("Visit.StudentId must be set before inserting a visit.");

            // No need for async/await wrapper — Db.InsertAsync already returns Task<int>.
            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.InsertAsync(visit);
            }, ct);
        }

        /// <summary>
        /// Update an existing Visit (by primary key).
        /// Returns rows affected (0 means not found).
        /// </summary>
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

            // Db.FindAsync is fine here because it queries by PK.
            // NOTE: This does not filter by Status; caller decides what "visible" means.
            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.FindAsync<Visit>(visitId);
            }, ct);
        }

        /// <summary>
        /// Delete a visit row (hard delete).
        /// NOTE: If you later decide you want soft-delete for visits too,
        /// add an IsDeleted flag to Visit and convert this method.
        /// </summary>
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
                var (start, end) = DateRanges.GetThisWeekRange();
                var query = Db.Table<Visit>()
                              .Where(v => v.ScheduledDateTime >= start && v.ScheduledDateTime < end);

                if (!includeCanceled)
                    query = query.Where(v => v.Status != VisitStatus.Canceled);

                return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
            }, ct);

       

        /// <summary>
        /// Cancels a visit (keeps history; does not delete). Safe no-op if not found.
        /// </summary>
        public Task CancelVisitAsync(int visitId, string? reason = null, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var visit = await Db.Table<Visit>()
                                    .Where(v => v.Id == visitId)
                                    .FirstOrDefaultAsync()
                                    .ConfigureAwait(false);

                if (visit is null)
                    return;

                visit.Status = VisitStatus.Canceled; // canonical spelling in your enum

                // Optional: only if your Visit model has Notes (it does, per DTO projection)
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    // Keep it simple and non-destructive.
                    // If you prefer a structured format, we can standardize later.
                    visit.Notes = string.IsNullOrWhiteSpace(visit.Notes)
                        ? $"[Canceled] {reason}"
                        : $"{visit.Notes}\n\n[Canceled] {reason}";
                }

                await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);


        /// <summary>
        /// Next future scheduled visit for a student (Scheduled only).
        /// Returns null if none exists.
        /// </summary>
        public Task<Visit?> GetNextFutureVisitForStudentAsync(int studentId, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var now = DateTime.Now;

                return Db.Table<Visit>()
                         .Where(v => v.StudentId == studentId &&
                                     v.Status == VisitStatus.Scheduled &&
                                     v.ScheduledDateTime >= now)
                         .OrderBy(v => v.ScheduledDateTime)
                         .FirstOrDefaultAsync();
            }, ct);

       
       
    }

}
