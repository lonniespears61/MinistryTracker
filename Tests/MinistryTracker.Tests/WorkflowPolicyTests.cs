using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Services;

namespace MinistryTracker.Tests;

public sealed class WorkflowPolicyTests
{
    [Theory]
    [InlineData(StudentStatus.Active, false, true)]
    [InlineData(StudentStatus.Paused, false, false)]
    [InlineData(StudentStatus.Completed, false, false)]
    [InlineData(StudentStatus.Discontinued, false, false)]
    [InlineData(StudentStatus.Active, true, false)]
    public void Student_schedule_policy_is_explicit(
        StudentStatus status,
        bool deleted,
        bool expected)
    {
        var student = new Student { Status = status, IsDeleted = deleted };
        Assert.Equal(expected, WorkflowPolicy.CanScheduleStudent(student));
    }

    [Theory]
    [InlineData(VisitStatus.Scheduled, true, false, true, true, false)]
    [InlineData(VisitStatus.Scheduled, false, true, false, false, false)]
    [InlineData(VisitStatus.Successful, false, true, false, false, true)]
    [InlineData(VisitStatus.Missed, false, true, false, true, true)]
    [InlineData(VisitStatus.CanceledByMe, false, false, false, false, true)]
    [InlineData(VisitStatus.CanceledByThem, false, false, false, false, true)]
    [InlineData(VisitStatus.Rescheduled, false, false, false, false, false)]
    public void Visit_status_matrix_is_explicit(
        VisitStatus status,
        bool future,
        bool canSetOutcome,
        bool canCancel,
        bool canReschedule,
        bool canScheduleNext)
    {
        var now = new DateTime(2026, 6, 20, 12, 0, 0);
        var scheduled = future ? now.AddHours(1) : now.AddHours(-1);
        var active = new Student { Status = StudentStatus.Active };

        Assert.Equal(
            canSetOutcome,
            WorkflowPolicy.CanSetVisitOutcome(status, scheduled, now));
        Assert.Equal(
            canCancel,
            WorkflowPolicy.CanCancelVisit(status, scheduled, now));
        Assert.Equal(
            canReschedule,
            WorkflowPolicy.CanRescheduleVisit(status, scheduled, now));
        Assert.Equal(
            canScheduleNext,
            WorkflowPolicy.CanScheduleNextVisit(active, status));
        Assert.Equal(
            status != VisitStatus.Rescheduled,
            WorkflowPolicy.CanEditVisitDetails(status));
    }
}
