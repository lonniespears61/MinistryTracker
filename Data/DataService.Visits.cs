// ---------------------------------------------------------------------------------------------------------------------
// DataService.Visits.cs
//
// PURPOSE
// - Visit-focused CRUD operations and visit query helpers.
// - Keeps Visit access aligned with the current "one visit = one attempt" model.
//
// DESIGN RULES
// - One Visit record = one scheduled attempt.
// - Reschedule an upcoming visit by closing it as Rescheduled and creating a new Visit record.
// - Rescheduling a Missed visit preserves the Missed outcome and creates a linked follow-up.
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
using MinistryTracker.Models.DTOs;
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

            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var inserted = 0;

                await Db.RunInTransactionAsync(connection =>
                {
                    if (visit.Status == VisitStatus.Scheduled)
                    {
                        ThrowIfStudentCannotBeScheduled(connection, visit.StudentId);
                        ThrowIfScheduleConflict(FindVisitScheduleConflict(
                            connection,
                            visit.StudentId,
                            visit.ScheduledDateTime,
                            excludeVisitId: null));
                    }

                    inserted = connection.Insert(visit);
                }).ConfigureAwait(false);

                return inserted;
            }, ct);
        }

        /// <summary>
        /// Atomically replaces one upcoming scheduled visit with another.
        /// </summary>
        public Task<int> ReplaceScheduledVisitAsync(
            int existingVisitId,
            Visit replacement,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(replacement);

            if (existingVisitId <= 0)
                throw new InvalidOperationException("Existing visit id must be set.");

            if (replacement.StudentId <= 0)
                throw new InvalidOperationException("Visit.StudentId must be set.");

            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var inserted = 0;

                await Db.RunInTransactionAsync(connection =>
                {
                    var existing = connection.Find<Visit>(existingVisitId)
                        ?? throw new InvalidOperationException("Existing visit not found.");

                    if (existing.Status != VisitStatus.Scheduled ||
                        existing.ScheduledDateTime < DateTime.Now)
                    {
                        throw new InvalidOperationException(
                            "Only an upcoming scheduled visit can be replaced.");
                    }

                    if (existing.StudentId != replacement.StudentId)
                        throw new InvalidOperationException("Replacement student does not match.");

                    ThrowIfStudentCannotBeScheduled(connection, replacement.StudentId);
                    ThrowIfScheduleConflict(FindVisitScheduleConflict(
                        connection,
                        replacement.StudentId,
                        replacement.ScheduledDateTime,
                        excludeVisitId: existing.Id));

                    existing.Status = VisitStatus.CanceledByMe;
                    const string reason = "Replaced by new visit";
                    existing.Notes = string.IsNullOrWhiteSpace(existing.Notes)
                        ? reason
                        : $"{existing.Notes}\n\n{reason}";
                    existing.NotesCreatedDateTime = DateTime.Now;

                    connection.Update(existing);
                    inserted = connection.Insert(replacement);
                }).ConfigureAwait(false);

                return inserted;
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
        /// Returns the complete visit history for one student, newest first.
        /// All statuses remain visible as part of the student's history.
        /// </summary>
        public Task<List<Visit>> GetVisitsForStudentAsync(
            int studentId,
            CancellationToken ct = default)
            => EnsureInitThen(() =>
                Db.Table<Visit>()
                  .Where(v => v.StudentId == studentId)
                  .OrderByDescending(v => v.ScheduledDateTime)
                  .ToListAsync(), ct);

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
        /// Returns the first conflict that would prevent scheduling a visit.
        /// A student may have only one future scheduled visit, and all scheduled
        /// visits must be at least 30 minutes apart.
        /// </summary>
        public Task<VisitScheduleConflict?> GetVisitScheduleConflictAsync(
            int studentId,
            DateTime? scheduledDateTime = null,
            int? excludeVisitId = null,
            CancellationToken ct = default)
            => EnsureInitThen(
                () => FindVisitScheduleConflictAsync(
                    studentId,
                    scheduledDateTime,
                    excludeVisitId),
                ct);

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

                if (visit.Status == VisitStatus.Rescheduled)
                    throw new InvalidOperationException("A rescheduled history record cannot be edited.");

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

                if (visit.ScheduledDateTime > DateTime.Now)
                    throw new InvalidOperationException("A future visit cannot be marked successful.");

                if (visit.Status is VisitStatus.CanceledByMe or
                    VisitStatus.CanceledByThem or
                    VisitStatus.Rescheduled)
                {
                    throw new InvalidOperationException(
                        "A canceled or rescheduled visit cannot be marked successful.");
                }

                visit.Status = VisitStatus.Successful;
                visit.CompletedDateTime =
                    completedDateTime ??
                    visit.CompletedDateTime ??
                    visit.ScheduledDateTime;

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

                if (visit.ScheduledDateTime > DateTime.Now)
                    throw new InvalidOperationException("A future visit cannot be marked missed.");

                if (visit.Status is VisitStatus.CanceledByMe or
                    VisitStatus.CanceledByThem or
                    VisitStatus.Rescheduled)
                {
                    throw new InvalidOperationException(
                        "A canceled or rescheduled visit cannot be marked missed.");
                }

                visit.Status = VisitStatus.Missed;
                visit.CompletedDateTime = null;

                if (notes is not null)
                {
                    visit.Notes = notes;
                    visit.NotesCreatedDateTime = DateTime.Now;
                }

                return await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);

        /// <summary>
        /// Atomically saves editable details and applies a completed visit outcome.
        /// </summary>
        public Task<int> UpdateVisitDetailsAndOutcomeAsync(
            int visitId,
            ContactMethod method,
            string? meetingAddress,
            string? notes,
            VisitStatus outcome,
            DateTime? completedDateTime = null,
            CancellationToken ct = default)
        {
            if (outcome is not VisitStatus.Successful and not VisitStatus.Missed)
                throw new InvalidOperationException("Outcome must be Successful or Missed.");

            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();
                var updated = 0;

                await Db.RunInTransactionAsync(connection =>
                {
                    var visit = connection.Find<Visit>(visitId);
                    if (visit is null)
                        return;

                    if (visit.ScheduledDateTime > DateTime.Now)
                        throw new InvalidOperationException("A future visit cannot have an outcome.");

                    if (visit.Status is VisitStatus.CanceledByMe or
                        VisitStatus.CanceledByThem or
                        VisitStatus.Rescheduled)
                    {
                        throw new InvalidOperationException(
                            "A canceled or rescheduled visit cannot have an outcome.");
                    }

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
                    visit.Status = outcome;
                    visit.CompletedDateTime = outcome == VisitStatus.Successful
                        ? completedDateTime ?? visit.CompletedDateTime ?? visit.ScheduledDateTime
                        : null;

                    updated = connection.Update(visit);
                }).ConfigureAwait(false);

                return updated;
            }, ct);
        }

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

                if (visit.Status != VisitStatus.Scheduled)
                    throw new InvalidOperationException("Only a scheduled visit can be canceled.");

                if (visit.ScheduledDateTime < DateTime.Now)
                    throw new InvalidOperationException(
                        "A past visit should be marked successful or missed instead of canceled.");

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
        /// Reschedule by creating a new scheduled visit linked back to the current record.
        /// Upcoming scheduled visits are closed as Rescheduled; Missed visits remain Missed.
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
                ct.ThrowIfCancellationRequested();
                Visit? replacement = null;

                await Db.RunInTransactionAsync(connection =>
                {
                    var current = connection.Find<Visit>(visitId)
                        ?? throw new InvalidOperationException("Visit not found.");

                    var canReschedule =
                        current.Status == VisitStatus.Missed ||
                        (current.Status == VisitStatus.Scheduled &&
                         current.ScheduledDateTime >= DateTime.Now);

                    if (!canReschedule)
                    {
                        throw new InvalidOperationException(
                            "Only an upcoming scheduled visit or a missed visit can be rescheduled.");
                    }

                    ThrowIfStudentCannotBeScheduled(connection, current.StudentId);
                    ThrowIfScheduleConflict(FindVisitScheduleConflict(
                        connection,
                        current.StudentId,
                        newScheduledDateTime,
                        current.Id));

                    if (current.Status == VisitStatus.Scheduled)
                        current.Status = VisitStatus.Rescheduled;

                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        current.Notes = string.IsNullOrWhiteSpace(current.Notes)
                            ? note
                            : $"{current.Notes}\n\n{note}";
                        current.NotesCreatedDateTime = DateTime.Now;
                    }

                    connection.Update(current);

                    replacement = new Visit
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

                    connection.Insert(replacement);
                }).ConfigureAwait(false);

                return replacement
                    ?? throw new InvalidOperationException("Replacement visit was not created.");
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

                if (visit.Status == VisitStatus.Rescheduled)
                    throw new InvalidOperationException("A rescheduled history record cannot be edited.");

                visit.Notes = notes;
                visit.NotesCreatedDateTime = string.IsNullOrWhiteSpace(notes) ? null : DateTime.Now;

                return await Db.UpdateAsync(visit).ConfigureAwait(false);
            }, ct);

        private async Task<VisitScheduleConflict?> FindVisitScheduleConflictAsync(
            int studentId,
            DateTime? scheduledDateTime,
            int? excludeVisitId)
        {
            var now = DateTime.Now;
            var futureVisits = await Db.Table<Visit>()
                .Where(v =>
                    v.StudentId == studentId &&
                    v.Status == VisitStatus.Scheduled &&
                    v.ScheduledDateTime >= now)
                .OrderBy(v => v.ScheduledDateTime)
                .ToListAsync()
                .ConfigureAwait(false);

            var existingFutureVisit = futureVisits.FirstOrDefault(
                v => excludeVisitId is null || v.Id != excludeVisitId.Value);

            if (existingFutureVisit is not null)
            {
                return new VisitScheduleConflict(
                    VisitScheduleConflictType.ExistingFutureVisit,
                    existingFutureVisit);
            }

            if (scheduledDateTime is null)
                return null;

            var windowStart = scheduledDateTime.Value.AddMinutes(-30);
            var windowEnd = scheduledDateTime.Value.AddMinutes(30);

            var nearbyVisits = await Db.Table<Visit>()
                .Where(v =>
                    v.Status == VisitStatus.Scheduled &&
                    v.ScheduledDateTime > windowStart &&
                    v.ScheduledDateTime < windowEnd)
                .OrderBy(v => v.ScheduledDateTime)
                .ToListAsync()
                .ConfigureAwait(false);

            var nearbyVisit = nearbyVisits.FirstOrDefault(
                v => excludeVisitId is null || v.Id != excludeVisitId.Value);

            return nearbyVisit is null
                ? null
                : new VisitScheduleConflict(
                    VisitScheduleConflictType.TimeSpacing,
                    nearbyVisit);
        }

        private static VisitScheduleConflict? FindVisitScheduleConflict(
            SQLite.SQLiteConnection connection,
            int studentId,
            DateTime? scheduledDateTime,
            int? excludeVisitId)
        {
            var now = DateTime.Now;
            var futureVisits = connection.Table<Visit>()
                .Where(v =>
                    v.StudentId == studentId &&
                    v.Status == VisitStatus.Scheduled &&
                    v.ScheduledDateTime >= now)
                .OrderBy(v => v.ScheduledDateTime)
                .ToList();

            var existingFutureVisit = futureVisits.FirstOrDefault(
                v => excludeVisitId is null || v.Id != excludeVisitId.Value);

            if (existingFutureVisit is not null)
            {
                return new VisitScheduleConflict(
                    VisitScheduleConflictType.ExistingFutureVisit,
                    existingFutureVisit);
            }

            if (scheduledDateTime is null)
                return null;

            var windowStart = scheduledDateTime.Value.AddMinutes(-30);
            var windowEnd = scheduledDateTime.Value.AddMinutes(30);

            var nearbyVisit = connection.Table<Visit>()
                .Where(v =>
                    v.Status == VisitStatus.Scheduled &&
                    v.ScheduledDateTime > windowStart &&
                    v.ScheduledDateTime < windowEnd)
                .OrderBy(v => v.ScheduledDateTime)
                .ToList()
                .FirstOrDefault(v => excludeVisitId is null || v.Id != excludeVisitId.Value);

            return nearbyVisit is null
                ? null
                : new VisitScheduleConflict(
                    VisitScheduleConflictType.TimeSpacing,
                    nearbyVisit);
        }

        private static void ThrowIfStudentCannotBeScheduled(
            SQLite.SQLiteConnection connection,
            int studentId)
        {
            var student = connection.Find<Student>(studentId)
                ?? throw new InvalidOperationException("Student not found.");

            if (student.IsDeleted)
                throw new InvalidOperationException("A deleted student cannot have a visit scheduled.");

            if (student.Status != StudentStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Visits can only be scheduled for active students. Current status: {student.Status}.");
            }
        }

        private static void ThrowIfScheduleConflict(VisitScheduleConflict? conflict)
        {
            if (conflict is null)
                return;

            var when = conflict.Visit.ScheduledDateTime;
            var message = conflict.Type == VisitScheduleConflictType.ExistingFutureVisit
                ? $"This student already has a visit scheduled for {when:ddd, MMM d} at {when:h:mm tt}."
                : $"You already have a visit scheduled within 30 minutes of {when:h:mm tt}.";

            throw new InvalidOperationException(message);
        }
    }
}
