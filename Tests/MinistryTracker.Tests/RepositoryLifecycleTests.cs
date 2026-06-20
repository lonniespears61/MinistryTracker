using MinistryTracker.Models.Enums;

namespace MinistryTracker.Tests;

public sealed class RepositoryLifecycleTests
{
    [Fact]
    public async Task Replacement_success_closes_original_and_creates_new_visit()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var original = TestDatabase.NewVisit(
            student.StudentId,
            DateTime.Now.AddDays(1).Date.AddHours(10));
        await db.Service.AddVisitAsync(original);

        var replacement = TestDatabase.NewVisit(
            student.StudentId,
            DateTime.Now.AddDays(2).Date.AddHours(10));

        await db.Service.ReplaceScheduledVisitAsync(original.Id, replacement);

        var closed = await db.Service.GetVisitByIdAsync(original.Id);
        Assert.Equal(VisitStatus.CanceledByMe, closed?.Status);
        Assert.Contains("Replaced by new visit", closed?.Notes);
        Assert.True(replacement.Id > 0);
        Assert.Equal(2, (await db.Service.GetVisitsForStudentAsync(student.StudentId)).Count);
    }

    [Fact]
    public async Task Reschedule_success_closes_scheduled_original_and_links_replacement()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var original = TestDatabase.NewVisit(
            student.StudentId,
            DateTime.Now.AddDays(1).Date.AddHours(10));
        await db.Service.AddVisitAsync(original);

        var replacement = await db.Service.RescheduleVisitAsync(
            original.Id,
            DateTime.Now.AddDays(2).Date.AddHours(11),
            note: "New day");

        Assert.Equal(
            VisitStatus.Rescheduled,
            (await db.Service.GetVisitByIdAsync(original.Id))?.Status);
        Assert.Equal(original.Id, replacement.RescheduledFromVisitId);
        Assert.Equal(VisitStatus.Scheduled, replacement.Status);
    }

    [Fact]
    public async Task Rescheduling_missed_visit_preserves_missed_history()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var missed = TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(-1));
        missed.Status = VisitStatus.Missed;
        await db.OpenRaw().InsertAsync(missed);

        var replacement = await db.Service.RescheduleVisitAsync(
            missed.Id,
            DateTime.Now.AddDays(1).Date.AddHours(10));

        Assert.Equal(
            VisitStatus.Missed,
            (await db.Service.GetVisitByIdAsync(missed.Id))?.Status);
        Assert.Equal(missed.Id, replacement.RescheduledFromVisitId);
    }

    [Fact]
    public async Task Outcome_success_saves_details_and_status_together()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var visit = TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(-1));
        await db.OpenRaw().InsertAsync(visit);

        await db.Service.UpdateVisitDetailsAndOutcomeAsync(
            visit.Id,
            ContactMethod.Phone,
            "Kingdom Hall",
            "Completed",
            VisitStatus.Successful,
            visit.ScheduledDateTime);

        var completed = await db.Service.GetVisitByIdAsync(visit.Id);
        Assert.Equal(VisitStatus.Successful, completed?.Status);
        Assert.Equal(ContactMethod.Phone, completed?.Method);
        Assert.Equal("Kingdom Hall", completed?.MeetingAddress);
        Assert.Equal("Completed", completed?.Notes);
        Assert.Equal(visit.ScheduledDateTime, completed?.CompletedDateTime);
    }

    [Fact]
    public async Task Missed_outcome_clears_completed_datetime()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var visit = TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(-1));
        visit.Status = VisitStatus.Successful;
        visit.CompletedDateTime = visit.ScheduledDateTime;
        await db.OpenRaw().InsertAsync(visit);

        await db.Service.UpdateVisitDetailsAndOutcomeAsync(
            visit.Id,
            ContactMethod.InPerson,
            null,
            null,
            VisitStatus.Missed);

        var missed = await db.Service.GetVisitByIdAsync(visit.Id);
        Assert.Equal(VisitStatus.Missed, missed?.Status);
        Assert.Null(missed?.CompletedDateTime);
    }
}
