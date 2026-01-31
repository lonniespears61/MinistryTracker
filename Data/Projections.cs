// ---------------------------------------------------------------------------------------------------------------------
// DataService.Projections.cs
// Cross-table projections / DTO helpers (joins done in memory for small datasets).
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
        /// WEEK-BASED DTO helper: visits for the current week joined with Student (UI-friendly).
        /// </summary>
        public Task<List<VisitWithStudent>> GetVisitsWithStudentsThisWeekAsync(
            bool includeCanceled = true,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var (start, end) = DateRanges.GetThisWeekRange();

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
        /// Returns all future scheduled visits across all students (sorted ascending),
        /// including a few student fields needed for list/calendar UI.
        ///
        /// "Future" = ScheduledDateTime > now (local time).
        ///
        /// NOTE:
        /// - Soft-deleted students are excluded (IsDeleted = 0).
        /// - Canceled visits are excluded by default.
        /// </summary>
        public Task<List<VisitWithStudent>> GetFutureVisitsWithStudentsAsync(
            DateTime? nowOverride = null,
            bool includeCanceled = false,
            CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var now = nowOverride ?? DateTime.Now;

                // sqlite-net stores enums as integers.
                // So we pass the int value for "Canceled" into the SQL parameter list.
                var canceledStatus = (int)VisitStatus.Canceled;

                // Column aliases MUST match VisitWithStudent property names.
                // Tables:
                //   Visit     -> [Table("Visit")] in Visit.cs
                //   Students  -> [Table("Students")] in Student.cs
                //
                // IMPORTANT:
                // - s.IsDeleted = 0 keeps soft-deleted students out of the calendar.
                // - includeCanceled controls whether canceled visits appear.
                const string sql = @"
SELECT
    v.Id                        AS VisitId,
    v.StudentId                 AS StudentId,
    s.Name                      AS StudentName,
    v.ScheduledDateTime         AS ScheduledDateTime,
    v.Status                    AS Status,
    v.VisitType                 AS VisitType,
    CASE
        WHEN v.Notes IS NULL THEN ''
        ELSE trim(substr(v.Notes, 1, 80))
    END                         AS NotesPreview
FROM Visit v
JOIN Students s ON s.StudentId = v.StudentId
WHERE
    s.IsDeleted = 0
    AND v.ScheduledDateTime > ?
    AND (? = 1 OR v.Status != ?)
ORDER BY v.ScheduledDateTime ASC;";

                // Params correspond to:
                // 1) now
                // 2) includeCanceled as 0/1
                // 3) canceledStatus int
                var includeCanceledInt = includeCanceled ? 1 : 0;

                return await Db.QueryAsync<VisitWithStudent>(
                    sql,
                    now,
                    includeCanceledInt,
                    canceledStatus
                ).ConfigureAwait(false);

            }, ct);
        }
    }
}