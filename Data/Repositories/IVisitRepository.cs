using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data.Repositories;

public interface IVisitRepository
{
    Task<List<Visit>> GetVisitsBetweenAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        bool includeCanceled = false,
        CancellationToken ct = default);
    Task<int> AddVisitAsync(Visit visit, CancellationToken ct = default);
    Task<Visit?> GetVisitByIdAsync(int visitId, CancellationToken ct = default);
    Task<List<Visit>> GetVisitsForStudentAsync(int studentId, CancellationToken ct = default);
    Task<int> DeleteVisitAsync(int visitId, CancellationToken ct = default);
    Task<List<Visit>> GetVisitsTodayAsync(CancellationToken ct = default);
    Task<List<Visit>> GetUpcomingVisitsAsync(int days = 7, CancellationToken ct = default);
    Task<List<Visit>> GetVisitsThisWeekAsync(bool includeCanceled = true, CancellationToken ct = default);
    Task<Visit?> GetNextFutureVisitForStudentAsync(int studentId, CancellationToken ct = default);
    Task<VisitScheduleConflict?> GetVisitScheduleConflictAsync(
        int studentId,
        DateTime? scheduledDateTime = null,
        int? excludeVisitId = null,
        CancellationToken ct = default);
    Task<List<Visit>> GetUnhandledMissedVisitsAsync(int days = 7, CancellationToken ct = default);
    Task<int> UpdateVisitDetailsAsync(
        int visitId,
        ContactMethod method,
        string? meetingAddress,
        string? notes,
        CancellationToken ct = default);
    Task<int> MarkVisitSuccessfulAsync(
        int visitId,
        string? notes = null,
        DateTime? completedDateTime = null,
        CancellationToken ct = default);
    Task<int> MarkVisitMissedAsync(
        int visitId,
        string? notes = null,
        CancellationToken ct = default);
    Task<int> CancelVisitByMeAsync(int visitId, string? reason = null, CancellationToken ct = default);
    Task<int> CancelVisitByThemAsync(int visitId, string? reason = null, CancellationToken ct = default);
    Task<Visit> RescheduleVisitAsync(
        int visitId,
        DateTime newScheduledDateTime,
        string? newMeetingAddress = null,
        double? newMeetingLatitude = null,
        double? newMeetingLongitude = null,
        string? note = null,
        CancellationToken ct = default);
    Task<int> UpdateVisitNotesAsync(int visitId, string? notes, CancellationToken ct = default);
    Task<List<VisitWithStudent>> GetVisitsWithStudentsInRangeAsync(
        DateTime startInclusive,
        DateTime endExclusive,
        bool includeCanceled = true,
        CancellationToken ct = default);
    Task<List<VisitWithStudent>> GetVisitsWithStudentsThisWeekAsync(
        bool includeCanceled = true,
        CancellationToken ct = default);
    Task<List<VisitWithStudent>> GetFutureVisitsWithStudentsAsync(
        DateTime? nowOverride = null,
        bool includeCanceled = false,
        CancellationToken ct = default);
}
