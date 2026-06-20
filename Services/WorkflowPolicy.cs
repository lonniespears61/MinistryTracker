using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Services;

public static class WorkflowPolicy
{
    public static bool CanScheduleStudent(Student? student) =>
        student is { IsDeleted: false, Status: StudentStatus.Active };

    public static bool CanEditVisitDetails(VisitStatus status) =>
        status != VisitStatus.Rescheduled;

    public static bool CanSetVisitOutcome(
        VisitStatus status,
        DateTime scheduledDateTime,
        DateTime now) =>
        scheduledDateTime <= now &&
        status is VisitStatus.Scheduled or VisitStatus.Successful or VisitStatus.Missed;

    public static bool CanCancelVisit(
        VisitStatus status,
        DateTime scheduledDateTime,
        DateTime now) =>
        status == VisitStatus.Scheduled &&
        scheduledDateTime >= now;

    public static bool CanRescheduleVisit(
        VisitStatus status,
        DateTime scheduledDateTime,
        DateTime now) =>
        status == VisitStatus.Missed ||
        (status == VisitStatus.Scheduled && scheduledDateTime >= now);

    public static bool CanScheduleNextVisit(
        Student? student,
        VisitStatus status) =>
        CanScheduleStudent(student) &&
        status is VisitStatus.Successful or
            VisitStatus.Missed or
            VisitStatus.CanceledByMe or
            VisitStatus.CanceledByThem;

    public static string GetVisitGuidance(
        VisitStatus status,
        DateTime scheduledDateTime,
        DateTime now) =>
        status switch
        {
            VisitStatus.Rescheduled =>
                "This is the original record of a rescheduled visit and is read-only.",
            VisitStatus.CanceledByMe or VisitStatus.CanceledByThem =>
                "This visit is closed. Details and notes may be corrected, or a next visit may be scheduled.",
            VisitStatus.Successful =>
                "This visit is complete. Correct the outcome or schedule the next visit.",
            VisitStatus.Missed =>
                "Correct the outcome, reschedule this attempt, or schedule the next visit.",
            VisitStatus.Scheduled when scheduledDateTime <= now =>
                "Record the outcome before scheduling the next visit.",
            VisitStatus.Scheduled =>
                "Edit, cancel, or reschedule this upcoming visit.",
            _ => string.Empty
        };
}
