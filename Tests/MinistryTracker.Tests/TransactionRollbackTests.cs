using MinistryTracker.Models.Enums;

namespace MinistryTracker.Tests;

public sealed class TransactionRollbackTests
{
    [Fact]
    public async Task Replacement_failure_preserves_original_visit()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var original = TestDatabase.NewVisit(
            student.StudentId,
            DateTime.Now.AddDays(1).Date.AddHours(10));
        await db.Service.AddVisitAsync(original);

        var raw = db.OpenRaw();
        await raw.ExecuteAsync(
            """
            CREATE TRIGGER ForceReplacementFailure
            BEFORE INSERT ON Visits
            WHEN NEW.ProtectionVersion = 1
            BEGIN
                SELECT RAISE(ABORT, 'forced replacement failure');
            END;
            """);

        var replacement = TestDatabase.NewVisit(
            student.StudentId,
            DateTime.Now.AddDays(2).Date.AddHours(10));
        replacement.Notes = "FORCE_REPLACEMENT_FAILURE";

        await Assert.ThrowsAnyAsync<Exception>(
            () => db.Service.ReplaceScheduledVisitAsync(original.Id, replacement));

        var preserved = await db.Service.GetVisitByIdAsync(original.Id);
        Assert.Equal(VisitStatus.Scheduled, preserved?.Status);
        Assert.Single(await db.Service.GetVisitsForStudentAsync(student.StudentId));
    }

    [Fact]
    public async Task Reschedule_insert_failure_preserves_original_visit()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var original = TestDatabase.NewVisit(
            student.StudentId,
            DateTime.Now.AddDays(1).Date.AddHours(10));
        await db.Service.AddVisitAsync(original);

        var raw = db.OpenRaw();
        await raw.ExecuteAsync(
            """
            CREATE TRIGGER ForceRescheduleFailure
            BEFORE INSERT ON Visits
            WHEN NEW.RescheduledFromVisitId IS NOT NULL
            BEGIN
                SELECT RAISE(ABORT, 'forced reschedule failure');
            END;
            """);

        await Assert.ThrowsAnyAsync<Exception>(
            () => db.Service.RescheduleVisitAsync(
                original.Id,
                DateTime.Now.AddDays(2).Date.AddHours(10)));

        var preserved = await db.Service.GetVisitByIdAsync(original.Id);
        Assert.Equal(VisitStatus.Scheduled, preserved?.Status);
        Assert.Single(await db.Service.GetVisitsForStudentAsync(student.StudentId));
    }

    [Fact]
    public async Task Outcome_failure_rolls_back_detail_changes()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        var visit = TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(-1));
        visit.Notes = "Original";
        await db.Service.AddVisitAsync(visit);

        var raw = db.OpenRaw();
        await raw.ExecuteAsync(
            """
            CREATE TRIGGER ForceOutcomeFailure
            BEFORE UPDATE ON Visits
            WHEN NEW.Status = 1
            BEGIN
                SELECT RAISE(ABORT, 'forced outcome failure');
            END;
            """);

        await Assert.ThrowsAnyAsync<Exception>(
            () => db.Service.UpdateVisitDetailsAndOutcomeAsync(
                visit.Id,
                ContactMethod.Phone,
                "Changed address",
                "Changed notes",
                VisitStatus.Successful));

        var preserved = await db.Service.GetVisitByIdAsync(visit.Id);
        Assert.Equal(VisitStatus.Scheduled, preserved?.Status);
        Assert.Equal(ContactMethod.InPerson, preserved?.Method);
        Assert.Null(preserved?.MeetingAddress);
        Assert.Equal("Original", preserved?.Notes);
    }
}
