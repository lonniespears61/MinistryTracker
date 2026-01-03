// ---------------------------------------------------------------------------------------------------------------------
// DataService.MyCalendar.cs  (DROP-IN - corrected)
//
// PURPOSE:
// - One DB call for "MyCalendar" that returns future visits across all students
// - Uses the UI DTO VisitWithStudent (NOT stored in the DB)
//
// FIXES vs previous draft:
// ✅ Filters out soft-deleted students (s.IsDeleted = 0)
// ✅ Excludes canceled visits by default (optional includeCanceled flag)
// ✅ Keeps aliases aligned to VisitWithStudent DTO
// ✅ Keeps EnsureInitThen + CancellationToken standard
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums; // VisitStatus enum

namespace MinistryTracker.Data
{
    public partial class DataService
    {
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
