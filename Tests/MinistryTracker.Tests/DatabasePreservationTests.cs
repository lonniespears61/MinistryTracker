using MinistryTracker.Data;
using MinistryTracker.Models.Enums;
using SQLite;

namespace MinistryTracker.Tests;

public sealed class DatabasePreservationTests
{
    [Fact]
    public async Task Reopening_database_preserves_students_visits_and_schema_version()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync(name: "Preserved");
        var visit = TestDatabase.NewVisit(
            student.StudentId,
            DateTime.Now.AddDays(1).Date.AddHours(10));
        await db.Service.AddVisitAsync(visit);
        var path = db.Path;

        SQLiteAsyncConnection.ResetPool();

        var reopened = new DataService(path);
        await reopened.InitializeAsync();

        var loadedStudent = await reopened.GetStudentByIdAsync(student.StudentId);
        var loadedVisit = await reopened.GetVisitByIdAsync(visit.Id);

        Assert.Equal("Preserved", loadedStudent?.Name);
        Assert.Equal(VisitStatus.Scheduled, loadedVisit?.Status);
        Assert.Equal(reopened.GetAppSchemaVersion(), await reopened.GetDatabaseSchemaVersionAsync());
    }

    [Fact]
    public async Task Baseline_upgrade_from_version_zero_preserves_existing_rows()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "MinistryTracker.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "ministrytracker.db3");

        try
        {
            var raw = new SQLiteAsyncConnection(path);
            await raw.CreateTableAsync<MinistryTracker.Models.Student>();
            await raw.CreateTableAsync<MinistryTracker.Models.Visit>();

            var student = new MinistryTracker.Models.Student
            {
                Name = "Legacy",
                Status = StudentStatus.Active,
                FirstContactDate = DateTime.Today
            };
            await raw.InsertAsync(student);

            var visit = TestDatabase.NewVisit(
                student.StudentId,
                DateTime.Now.AddDays(1).Date.AddHours(11));
            await raw.InsertAsync(visit);
            await raw.ExecuteAsync("PRAGMA user_version = 0;");
            SQLiteAsyncConnection.ResetPool();

            var upgraded = new DataService(path);
            await upgraded.InitializeAsync();

            Assert.Equal(1, await upgraded.GetDatabaseSchemaVersionAsync());
            Assert.Equal("Legacy", (await upgraded.GetStudentByIdAsync(student.StudentId))?.Name);
            Assert.Equal(VisitStatus.Scheduled, (await upgraded.GetVisitByIdAsync(visit.Id))?.Status);
        }
        finally
        {
            SQLiteAsyncConnection.ResetPool();
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
            }
        }
    }
}
