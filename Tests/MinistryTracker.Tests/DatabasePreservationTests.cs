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

        var reopened = db.Reopen();
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
            await raw.ExecuteAsync(
                """
                CREATE TABLE Students (
                    StudentId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT, PhoneNumber TEXT, Email TEXT, PrimaryAddress TEXT,
                    PrimaryLatitude REAL, PrimaryLongitude REAL, PreferredLanguage TEXT,
                    Gender INTEGER, Age INTEGER, Notes TEXT
                );
                """);
            await raw.ExecuteAsync(
                """
                CREATE TABLE Visits (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MeetingAddress TEXT, MeetingLatitude REAL, MeetingLongitude REAL, Notes TEXT
                );
                """);
            await raw.ExecuteAsync(
                """
                INSERT INTO Students
                    (Name, PhoneNumber, Email, PrimaryAddress, PrimaryLatitude,
                     PrimaryLongitude, PreferredLanguage, Gender, Age, Notes)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                "Legacy", "555-0100", "legacy@example.com", "100 Main St",
                40.1, -88.2, "English", 0, 42, "Private student note");
            await raw.ExecuteAsync(
                """
                INSERT INTO Visits
                    (MeetingAddress, MeetingLatitude, MeetingLongitude, Notes)
                VALUES (?, ?, ?, ?)
                """,
                "200 Oak St", 40.2, -88.3, "Private visit note");
            await raw.ExecuteAsync("PRAGMA user_version = 0;");
            SQLiteAsyncConnection.ResetPool();

            var key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
            var upgraded = TestDatabase.CreateService(path, key);
            await upgraded.InitializeAsync();

            Assert.Equal(2, await upgraded.GetDatabaseSchemaVersionAsync());

            var student = await upgraded.GetStudentByIdAsync(1);
            var visit = await upgraded.GetVisitByIdAsync(1);
            Assert.Equal("Legacy", student?.Name);
            Assert.Equal("555-0100", student?.PhoneNumber);
            Assert.Equal("Private student note", student?.Notes);
            Assert.Equal("200 Oak St", visit?.MeetingAddress);
            Assert.Equal("Private visit note", visit?.Notes);

            var plaintextName = await raw.ExecuteScalarAsync<string>(
                "SELECT Name FROM Students WHERE StudentId = 1");
            var plaintextVisitNotes = await raw.ExecuteScalarAsync<string?>(
                "SELECT Notes FROM Visits WHERE Id = 1");
            Assert.Equal(string.Empty, plaintextName);
            Assert.Null(plaintextVisitNotes);

            SQLiteAsyncConnection.ResetPool();
            var databaseBytes = await File.ReadAllBytesAsync(path);
            var databaseText = System.Text.Encoding.UTF8.GetString(databaseBytes);
            Assert.DoesNotContain("Legacy", databaseText);
            Assert.DoesNotContain("Private student note", databaseText);
            Assert.DoesNotContain("Private visit note", databaseText);
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
