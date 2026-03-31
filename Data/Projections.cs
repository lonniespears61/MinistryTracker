// ---------------------------------------------------------------------------------------------------------------------
// DataService.Projections.cs
//
// PURPOSE
// - DTO helpers combining Visit + Student for UI (lists, calendar)
//
// DESIGN RULES
// - Small dataset → in-memory join is fine
// - Visit uses Stage (not VisitType)
// - Table name = Visits
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
        public Task<List<VisitWithStudent>> GetVisitsWithStudentsInRangeAsync(
            DateTime startInclusive,
            DateTime endExclusive,
            bool includeCanceled = true,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var visitsQuery = Db.Table<Visit>()
                                    .Where(v => v.ScheduledDateTime >= startInclusive &&
                                                v.ScheduledDateTime < endExclusive);

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
                        Stage = v.Stage,
                        NotesPreview = string.IsNullOrWhiteSpace(v.Notes)
                            ? string.Empty
                            : (v.Notes!.Length > 80 ? v.Notes[..80] + "…" : v.Notes)
                    });
                }

                return result.OrderBy(x => x.ScheduledDateTime).ToList();
            }, ct);

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
                        Stage = v.Stage,
                        NotesPreview = string.IsNullOrWhiteSpace(v.Notes)
                            ? string.Empty
                            : (v.Notes!.Length > 80 ? v.Notes[..80] + "…" : v.Notes)
                    });
                }

                return result.OrderBy(x => x.ScheduledDateTime).ToList();
            }, ct);

        public Task<List<VisitWithStudent>> GetFutureVisitsWithStudentsAsync(
            DateTime? nowOverride = null,
            bool includeCanceled = false,
            CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var now = nowOverride ?? DateTime.Now;
                var canceledStatus = (int)VisitStatus.Canceled;
                var includeCanceledInt = includeCanceled ? 1 : 0;

                const string sql = @"
SELECT
    v.Id                AS VisitId,
    v.StudentId         AS StudentId,
    s.Name              AS StudentName,
    v.ScheduledDateTime AS ScheduledDateTime,
    v.Status            AS Status,
    v.Stage             AS Stage,
    CASE
        WHEN v.Notes IS NULL THEN ''
        ELSE trim(substr(v.Notes, 1, 80))
    END                 AS NotesPreview
FROM Visits v
JOIN Students s ON s.StudentId = v.StudentId
WHERE
    s.IsDeleted = 0
    AND v.ScheduledDateTime > ?
    AND (? = 1 OR v.Status != ?)
ORDER BY v.ScheduledDateTime ASC;";

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