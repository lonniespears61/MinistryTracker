// ---------------------------------------------------------------------------------------------------------------------
// DataService.Visits.cs
//
// PURPOSE
// - Visit-focused CRUD operations and visit query helpers.
// - Keeps Visit access aligned with the current "one visit = one attempt" model.
//
// DESIGN RULES
// - One Visit record = one scheduled attempt.
// - Reschedule = close old visit as Rescheduled and create a new Visit record.
// - Cancellations preserve history; visits are not deleted as part of normal workflow.
// - Notes are stored as one field, with NotesCreatedDateTime tracking when note content was last updated.
// - This file does not apply UI prompts; it provides data operations that UI/workflow can build on.
//
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Returns visits scheduled between [startInclusive, endExclusive).
        /// Optionally includes canceled visits.
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
                {
                    query = query.Where(v =>
                        v.Status != VisitStatus.CanceledByMe &&
                        v.Status != VisitStatus.CanceledByThem);
                }

                return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
            }, ct);

        // =====================================================================
        // CRUD
        // =====================================================================

        /// <summary>
        /// Add a new visit.
        /// </summary>
        public Task<int> AddVisitAsync(Visit visit, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(visit);

            if (visit.StudentId <= 0)
                throw new InvalidOperationException("Visit.StudentId must be set.");

            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.InsertAsync(visit);
            }, ct);
        }

        /// <summary>
        /// Update an existing visit.
        /// </summary>
        public Task<int> UpdateVisitAsync(Visit visit, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(visit);

            if (visit.Id <= 0)
                throw new InvalidOperationException("Visit.Id must be set.");

            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.UpdateAsync(visit);
            }, ct);
        }

        /// <summary>
        /// Find a visit by primary key.
        /// </summary>
        public Task<Visit?> GetVisitByIdAsync(int visitId, CancellationToken ct = default)
            => EnsureInitThen<Visit?>(async () =>
            {
                var visit = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                return visit;
            }, ct);

        /// <summary>
        /// Delete a visit record.
        /// Reserved for true cleanup scenarios, not normal workflow.
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

        // =====================================================================
        // COMMON QUERIES
        // =====================================================================

        /// <summary>
        /// Returns visits scheduled for today.
        /// </summary>
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

        /// <summary>
        /// Returns upcoming scheduled visits in the next N days.
        /// </summary>
        public Task<List<Visit>> GetUpcomingVisitsAsync(int days = 7, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var start = DateTime.Now;
                var end = start.AddDays(days);

                return Db.Table<Visit>()
                         .Where(v =>
                             v.Status == VisitStatus.Scheduled &&
                             v.ScheduledDateTime >= start &&
                             v.ScheduledDateTime <= end)
                         .OrderBy(v => v.ScheduledDateTime)
                         .ToListAsync();
            }, ct);

        /// <summary>
        /// Returns visits in the current week.
        /// Optionally includes canceled visits.
        /// </summary>
        public Task<List<Visit>> GetVisitsThisWeekAsync(bool includeCanceled = true, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var (start, end) = DateRanges.GetThisWeekRange();

                var query = Db.Table<Visit>()
                              .Where(v => v.ScheduledDateTime >= start &&
                                          v.ScheduledDateTime < end);

                if (!includeCanceled)
                {
                    query = query.Where(v =>
                        v.Status != VisitStatus.CanceledByMe &&
                        v.Status != VisitStatus.CanceledByThem);
                }

                return query.OrderBy(v => v.ScheduledDateTime).ToListAsync();
            }, ct);

        /// <summary>
        /// Returns the next scheduled future visit for a student.
        /// </summary>
        public Task<Visit?> GetNextFutureVisitForStudentAsync(int studentId, CancellationToken ct = default)
            => EnsureInitThen<Visit?>(async () =>
            {
                var now = DateTime.Now;

                return await Db.Table<Visit>()
                               .Where(v =>
                                   v.StudentId == studentId &&
                                   v.Status == VisitStatus.Scheduled &&
                                   v.ScheduledDateTime >= now)
                               .OrderBy(v => v.ScheduledDateTime)
                               .FirstOrDefaultAsync()
                               .ConfigureAwait(false);
            }, ct);

        /// <summary>
        /// Returns missed visits from the last N days that still have no notes and no replacement visit.
        /// Used for "Missed Recently" dashboard behavior.
        /// </summary>
        public Task<List<Visit>> GetUnhandledMissedVisitsAsync(int days = 7, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var start = DateTime.Now.AddDays(-days);

                var missedVisits = await Db.Table<Visit>()
                                           .Where(v =>
                                               v.Status == VisitStatus.Missed &&
                                               v.ScheduledDateTime >= start)
                                           .OrderByDescending(v => v.ScheduledDateTime)
                                           .ToListAsync()
                                           .ConfigureAwait(false);

                if (missedVisits.Count == 0)
                    return missedVisits;

                var studentIds = missedVisits.Select(v => v.StudentId).Distinct().ToList();

                var futureScheduledVisits = await Db.Table<Visit>()
                                                   .Where(v =>
                                                       studentIds.Contains(v.StudentId) &&
                                                       v.Status == VisitStatus.Scheduled &&
                                                       v.ScheduledDateTime > DateTime.Now)
                                                   .ToListAsync()
                                                   .ConfigureAwait(false);

                var studentsWithFutureVisits = futureScheduledVisits
                    .Select(v => v.StudentId)
                    .Distinct()
                    .ToHashSet();

                return missedVisits
                    .Where(v =>
                        string.IsNullOrWhiteSpace(v.Notes) &&
                        !studentsWithFutureVisits.Contains(v.StudentId))
                    .ToList();
            }, ct);

        // =====================================================================
        // EDIT HELPERS
        // =====================================================================

        /// <summary>
        /// Update the editable details of an existing visit without changing its identity or schedule.
        ///
        /// WHY:
        /// - Same-record edits are allowed for notes, method, and meeting address.
        /// - Rescheduling is NOT handled here. Reschedule has its own workflow that
        ///   closes the old record as Rescheduled and creates a new Scheduled record.
        /// - Status changes like cancel/success/missed are also handled by dedicated methods.
        /// </summary>
        public Task<int> UpdateVisitDetailsAsync(
            int visitId,
            ContactMethod method,
            string? meetingAddress,
            string? notes,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var visit = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                if (visit is null)
                    return 0;

                visit.Method = method;
                visit.MeetingAddress = string.IsNullOrWhiteSpace(meetingAddress)
                    ? null
                    : meetingAddress.Trim();

                visit.Notes = string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim();

                visit.NotesCreatedDateTime = string.IsNullOrWhiteSpace(visit.Notes)
                    ? null
                    : DateTime.Now;

                return await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);

        // =====================================================================
        // OUTCOME / LIFECYCLE HELPERS
        // =====================================================================

        /// <summary>
        /// Mark a visit as successful and optionally stamp note timing.
        /// </summary>
        public Task<int> MarkVisitSuccessfulAsync(
            int visitId,
            string? notes = null,
            DateTime? completedDateTime = null,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var visit = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                if (visit is null)
                    return 0;

                visit.Status = VisitStatus.Successful;
                visit.CompletedDateTime = completedDateTime ?? DateTime.Now;

                if (notes is not null)
                {
                    visit.Notes = notes;
                    visit.NotesCreatedDateTime = DateTime.Now;
                }

                return await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);

        /// <summary>
        /// Mark a visit as missed.
        /// </summary>
        public Task<int> MarkVisitMissedAsync(
            int visitId,
            string? notes = null,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var visit = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                if (visit is null)
                    return 0;

                visit.Status = VisitStatus.Missed;

                if (notes is not null)
                {
                    visit.Notes = notes;
                    visit.NotesCreatedDateTime = DateTime.Now;
                }

                return await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);

        /// <summary>
        /// Cancel a visit as canceled by the user.
        /// Preserves history and optionally appends context to notes.
        /// </summary>
        public Task<int> CancelVisitByMeAsync(int visitId, string? reason = null, CancellationToken ct = default)
            => CancelVisitAsync(visitId, VisitStatus.CanceledByMe, reason, ct);

        /// <summary>
        /// Cancel a visit as canceled by the other person.
        /// Preserves history and optionally appends context to notes.
        /// </summary>
        public Task<int> CancelVisitByThemAsync(int visitId, string? reason = null, CancellationToken ct = default)
            => CancelVisitAsync(visitId, VisitStatus.CanceledByThem, reason, ct);

        private Task<int> CancelVisitAsync(
            int visitId,
            VisitStatus canceledStatus,
            string? reason,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var visit = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                if (visit is null)
                    return 0;

                visit.Status = canceledStatus;

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    visit.Notes = string.IsNullOrWhiteSpace(visit.Notes)
                        ? reason
                        : $"{visit.Notes}\n\n{reason}";

                    visit.NotesCreatedDateTime = DateTime.Now;
                }

                return await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);

        /// <summary>
        /// Reschedule a visit by closing the current record as Rescheduled
        /// and creating a new scheduled visit linked back to it.
        /// </summary>
        public Task<Visit> RescheduleVisitAsync(
            int visitId,
            DateTime newScheduledDateTime,
            string? newMeetingAddress = null,
            double? newMeetingLatitude = null,
            double? newMeetingLongitude = null,
            string? note = null,
            CancellationToken ct = default)
        {
            if (newScheduledDateTime.Date < DateTime.Today)
                throw new InvalidOperationException("A visit cannot be rescheduled before today.");

            return EnsureInitThen(async () =>
            {
                var current = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                if (current is null)
                    throw new InvalidOperationException("Visit not found.");

                current.Status = VisitStatus.Rescheduled;

                if (!string.IsNullOrWhiteSpace(note))
                {
                    current.Notes = string.IsNullOrWhiteSpace(current.Notes)
                        ? note
                        : $"{current.Notes}\n\n{note}";
                    current.NotesCreatedDateTime = DateTime.Now;
                }

                await Db.UpdateAsync(current).ConfigureAwait(false);

                var replacement = new Visit
                {
                    StudentId = current.StudentId,
                    Method = current.Method,
                    ScheduledDateTime = newScheduledDateTime,
                    Status = VisitStatus.Scheduled,
                    MeetingAddress = newMeetingAddress ?? current.MeetingAddress,
                    MeetingLatitude = newMeetingLatitude ?? current.MeetingLatitude,
                    MeetingLongitude = newMeetingLongitude ?? current.MeetingLongitude,
                    RescheduledFromVisitId = current.Id,
                    Notes = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                    NotesCreatedDateTime = string.IsNullOrWhiteSpace(note) ? null : DateTime.Now
                };

                await Db.InsertAsync(replacement).ConfigureAwait(false);
                return replacement;
            }, ct);
        }

        /// <summary>
        /// Update visit notes and stamp note timing.
        /// </summary>
        public Task<int> UpdateVisitNotesAsync(int visitId, string? notes, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var visit = await Db.FindAsync<Visit>(visitId).ConfigureAwait(false);
                if (visit is null)
                    return 0;

                visit.Notes = notes;
                visit.NotesCreatedDateTime = string.IsNullOrWhiteSpace(notes) ? null : DateTime.Now;

                return await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);
    }
}
