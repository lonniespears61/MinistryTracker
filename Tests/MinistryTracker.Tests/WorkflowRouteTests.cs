using MinistryTracker.Services;

namespace MinistryTracker.Tests;

public sealed class WorkflowRouteTests
{
    [Fact]
    public void Known_student_starts_in_calendar()
    {
        Assert.Equal(
            "MyCalendarPage?mode=schedule&studentId=42",
            WorkflowRoutes.ScheduleCalendar(42));
    }

    [Fact]
    public void Replacement_context_survives_calendar_and_add_visit_routes()
    {
        Assert.Equal(
            "MyCalendarPage?mode=schedule&studentId=42&replaceVisitId=9",
            WorkflowRoutes.ScheduleCalendar(42, 9));

        Assert.Equal(
            "AddVisitPage?studentId=42&date=2026-06-20&cancelTo=calendar&replaceVisitId=9",
            WorkflowRoutes.AddVisit(
                42,
                new DateTime(2026, 6, 20),
                WorkflowRoutes.CancelToCalendar,
                9));
    }

    [Fact]
    public void Dashboard_and_calendar_origins_have_explicit_cancel_destinations()
    {
        Assert.Contains(
            "cancelTo=dashboard",
            WorkflowRoutes.AddVisit(
                42,
                new DateTime(2026, 6, 20),
                WorkflowRoutes.CancelToDashboard));

        Assert.Contains(
            "cancelTo=calendar",
            WorkflowRoutes.AddVisit(
                42,
                new DateTime(2026, 6, 20),
                WorkflowRoutes.CancelToCalendar));
    }

    [Fact]
    public void Supporting_routes_are_stable()
    {
        Assert.Equal(
            "SelectStudentForVisitPage?date=2026-06-20",
            WorkflowRoutes.SelectStudent(new DateTime(2026, 6, 20)));
        Assert.Equal("UpdateVisitPage?visitId=7", WorkflowRoutes.UpdateVisit(7));
        Assert.Equal(
            "StudentProfilePage?studentId=42",
            WorkflowRoutes.StudentProfile(42));
    }
}
