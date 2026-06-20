using MinistryTracker.Models.Enums;

namespace MinistryTracker.Tests;

public sealed class RepositoryRuleTests
{
    [Theory]
    [InlineData(StudentStatus.Paused, false)]
    [InlineData(StudentStatus.Completed, false)]
    [InlineData(StudentStatus.Discontinued, false)]
    [InlineData(StudentStatus.Active, true)]
    public async Task AddVisit_rejects_students_outside_active_workflow(
        StudentStatus status,
        bool isDeleted)
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync(status, isDeleted);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Service.AddVisitAsync(
                TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(1))));

        Assert.Contains(
            isDeleted ? "deleted student" : "active students",
            error.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.Service.GetVisitsForStudentAsync(student.StudentId));
    }

    [Fact]
    public async Task AddVisit_rejects_unknown_student()
    {
        await using var db = await TestDatabase.CreateAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Service.AddVisitAsync(
                TestDatabase.NewVisit(999999, DateTime.Now.AddDays(1))));

        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Student_can_have_only_one_future_scheduled_visit()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();

        await db.Service.AddVisitAsync(
            TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(1)));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Service.AddVisitAsync(
                TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(2))));

        Assert.Contains("already has a visit", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Visits_less_than_thirty_minutes_apart_are_rejected()
    {
        await using var db = await TestDatabase.CreateAsync();
        var firstStudent = await db.AddStudentAsync(name: "First");
        var secondStudent = await db.AddStudentAsync(name: "Second");
        var time = DateTime.Now.AddDays(1).Date.AddHours(10);

        await db.Service.AddVisitAsync(TestDatabase.NewVisit(firstStudent.StudentId, time));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Service.AddVisitAsync(
                TestDatabase.NewVisit(secondStudent.StudentId, time.AddMinutes(29))));
    }

    [Fact]
    public async Task Visits_exactly_thirty_minutes_apart_are_allowed()
    {
        await using var db = await TestDatabase.CreateAsync();
        var firstStudent = await db.AddStudentAsync(name: "First");
        var secondStudent = await db.AddStudentAsync(name: "Second");
        var time = DateTime.Now.AddDays(1).Date.AddHours(10);

        await db.Service.AddVisitAsync(TestDatabase.NewVisit(firstStudent.StudentId, time));
        var second = TestDatabase.NewVisit(secondStudent.StudentId, time.AddMinutes(30));

        var rows = await db.Service.AddVisitAsync(second);

        Assert.Equal(1, rows);
        Assert.True(second.Id > 0);
    }

    [Fact]
    public async Task Past_scheduled_visit_cannot_be_canceled()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var visit = TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(-1));
        await db.OpenRaw().InsertAsync(visit);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Service.CancelVisitByMeAsync(visit.Id));
    }

    [Fact]
    public async Task Future_visit_cannot_receive_outcome()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var visit = TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(1));
        await db.Service.AddVisitAsync(visit);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Service.UpdateVisitDetailsAndOutcomeAsync(
                visit.Id,
                ContactMethod.Phone,
                null,
                null,
                VisitStatus.Successful));
    }
}
