// ---------------------------------------------------------------------------------------------------------------------
// DataService.Projections.cs
//
// PURPOSE
// - DTO helpers combining Visit + Student for UI display (lists, dashboard, calendar)
//
// DESIGN RULES
// - Small dataset → in-memory join is acceptable
// - Visit no longer uses Stage or Type
// - Status drives lifecycle behavior
// - NotesPreview is only for lightweight display
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
                {
                    visitsQuery = visitsQuery.Where(v =>
                        v.Status != VisitStatus.CanceledByMe &&
                        v.Status != VisitStatus.CanceledByThem);
                }

                var visits = await visitsQuery.ToListAsync().ConfigureAwait(false);
                if (visits.Count == 0)
                    return new List<VisitWithStudent>();

                var studentIds = visits.Select(v => v.StudentId).Distinct().ToList();

                var students = await Db.Table<Student>()
                                       .Where(s => studentIds.Contains(s.StudentId) && !s.IsDeleted)
                                       .ToListAsync()
                                       .ConfigureAwait(false);

                var byId = students.ToDictionary(s => s.StudentId);

                var result = new List<VisitWithStudent>(visits.Count);

                foreach (var visit in visits)
                {
                    ct.ThrowIfCancellationRequested();

                    byId.TryGetValue(visit.StudentId, out var student);

                    result.Add(new VisitWithStudent
                    {
                        VisitId = visit.Id,
                        StudentId = visit.StudentId,
                        StudentName = student?.Name ?? "(Unnamed)",
                        ScheduledDateTime = visit.ScheduledDateTime,
                        Status = visit.Status,
                        NotesPreview = BuildNotesPreview(visit.Notes)
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
                {
                    visitsQuery = visitsQuery.Where(v =>
                        v.Status != VisitStatus.CanceledByMe &&
                        v.Status != VisitStatus.CanceledByThem);
                }

                var visits = await visitsQuery.ToListAsync().ConfigureAwait(false);
                if (visits.Count == 0)
                    return new List<VisitWithStudent>();

                var studentIds = visits.Select(v => v.StudentId).Distinct().ToList();

                var students = await Db.Table<Student>()
                                       .Where(s => studentIds.Contains(s.StudentId) && !s.IsDeleted)
                                       .ToListAsync()
                                       .ConfigureAwait(false);

                var byId = students.ToDictionary(s => s.StudentId);

                var result = new List<VisitWithStudent>(visits.Count);

                foreach (var visit in visits)
                {
                    ct.ThrowIfCancellationRequested();

                    byId.TryGetValue(visit.StudentId, out var student);

                    result.Add(new VisitWithStudent
                    {
                        VisitId = visit.Id,
                        StudentId = visit.StudentId,
                        StudentName = student?.Name ?? "(Unnamed)",
                        ScheduledDateTime = visit.ScheduledDateTime,
                        Status = visit.Status,
                        NotesPreview = BuildNotesPreview(visit.Notes)
                    });
                }

                return result.OrderBy(x => x.ScheduledDateTime).ToList();
            }, ct);

        public Task<List<VisitWithStudent>> GetFutureVisitsWithStudentsAsync(
            DateTime? nowOverride = null,
            bool includeCanceled = false,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var now = nowOverride ?? DateTime.Now;

                var visits = await Db.Table<Visit>()
                                     .Where(v =>
                                         v.ScheduledDateTime > now &&
                                         (includeCanceled ||
                                          (v.Status != VisitStatus.CanceledByMe &&
                                           v.Status != VisitStatus.CanceledByThem)))
                                     .OrderBy(v => v.ScheduledDateTime)
                                     .ToListAsync()
                                     .ConfigureAwait(false);

                if (visits.Count == 0)
                    return new List<VisitWithStudent>();

                var studentIds = visits.Select(v => v.StudentId).Distinct().ToList();

                var students = await Db.Table<Student>()
                                       .Where(s => studentIds.Contains(s.StudentId) && !s.IsDeleted)
                                       .ToListAsync()
                                       .ConfigureAwait(false);

                var byId = students.ToDictionary(s => s.StudentId);

                var result = new List<VisitWithStudent>(visits.Count);

                foreach (var visit in visits)
                {
                    ct.ThrowIfCancellationRequested();

                    byId.TryGetValue(visit.StudentId, out var student);

                    result.Add(new VisitWithStudent
                    {
                        VisitId = visit.Id,
                        StudentId = visit.StudentId,
                        StudentName = student?.Name ?? "(Unnamed)",
                        ScheduledDateTime = visit.ScheduledDateTime,
                        Status = visit.Status,
                        NotesPreview = BuildNotesPreview(visit.Notes)
                    });
                }

                return result;
            }, ct);

        private static string BuildNotesPreview(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return string.Empty;

            return notes.Length > 80
                ? notes[..80] + "…"
                : notes;
        }

        public Task<List<CheckOnStudentSuggestion>> GetCheckOnStudentSuggestionsAsync(
            int take = 5,
            int minDaysSinceVisit = 30,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var now = DateTime.Now;
                var cutoff = DateTime.Today.AddDays(-minDaysSinceVisit);

                var students = await Db.Table<Student>()
                    .Where(s => !s.IsDeleted && s.Status == StudentStatus.Active)
                    .ToListAsync()
                    .ConfigureAwait(false);

                if (students.Count == 0)
                    return new List<CheckOnStudentSuggestion>();

                var visits = await Db.Table<Visit>()
                    .ToListAsync()
                    .ConfigureAwait(false);

                var futureScheduledStudentIds = visits
                    .Where(v => v.Status == VisitStatus.Scheduled && v.ScheduledDateTime >= now)
                    .Select(v => v.StudentId)
                    .Distinct()
                    .ToHashSet();

                var suggestions = students
                    .Where(s => !futureScheduledStudentIds.Contains(s.StudentId))
                    .Select(s =>
                    {
                        var lastVisit = visits
                            .Where(v =>
                                v.StudentId == s.StudentId &&
                                v.ScheduledDateTime < now &&
                                v.Status != VisitStatus.CanceledByMe &&
                                v.Status != VisitStatus.CanceledByThem)
                            .OrderByDescending(v => v.ScheduledDateTime)
                            .FirstOrDefault();

                        return new CheckOnStudentSuggestion
                        {
                            StudentId = s.StudentId,
                            StudentName = s.Name ?? "(Unnamed)",
                            LastVisitDate = lastVisit?.ScheduledDateTime
                        };
                    })
                    .Where(x => x.LastVisitDate is null || x.LastVisitDate.Value.Date <= cutoff)
                    .OrderBy(x => x.LastVisitDate ?? DateTime.MinValue)
                    .ThenBy(x => x.StudentName)
                    .Take(Math.Clamp(take, 3, 5))
                    .ToList();

                return suggestions;
            }, ct);
    }
}
