using System.Security.Cryptography;
using MinistryTracker.Data.Security;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using SQLite;

namespace MinistryTracker.Tests;

public sealed class DataEncryptionTests
{
    [Fact]
    public async Task Aes_gcm_round_trip_is_randomized_and_detects_tampering()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var protection = new AesGcmDataProtectionService(() => Task.FromResult(key));
        await protection.InitializeAsync();

        var first = protection.Protect("private value");
        var second = protection.Protect("private value");

        Assert.NotEqual(first, second);
        Assert.Equal("private value", protection.Unprotect(first));

        var tampered = first![..^1] + (first[^1] == 'A' ? "B" : "A");
        Assert.ThrowsAny<CryptographicException>(() => protection.Unprotect(tampered));
    }

    [Fact]
    public async Task Sensitive_student_and_visit_values_are_not_stored_as_plaintext()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = new Student
        {
            Name = "Sensitive Person",
            PhoneNumber = "555-0199",
            Email = "sensitive@example.com",
            PrimaryAddress = "123 Private Lane",
            PrimaryLatitude = 40.123,
            PrimaryLongitude = -88.456,
            PreferredLanguage = "English",
            Gender = Gender.Female,
            Age = 37,
            Notes = "Private student notes",
            FirstContactDate = DateTime.Today,
            Status = StudentStatus.Active
        };
        await db.Service.AddStudentAsync(student);

        var visit = TestDatabase.NewVisit(student.StudentId, DateTime.Now.AddDays(1));
        visit.MeetingAddress = "456 Confidential Road";
        visit.MeetingLatitude = 40.5;
        visit.MeetingLongitude = -88.5;
        visit.Notes = "Private visit notes";
        await db.Service.AddVisitAsync(visit);

        var raw = db.OpenRaw();
        var rawStudent = await raw.QueryAsync<RawStudent>(
            "SELECT * FROM Students WHERE StudentId = ?", student.StudentId);
        var rawVisit = await raw.QueryAsync<RawVisit>(
            "SELECT * FROM Visits WHERE Id = ?", visit.Id);

        Assert.StartsWith("mtw1:", rawStudent.Single().ProtectedName);
        Assert.DoesNotContain("Sensitive Person", rawStudent.Single().ProtectedName);
        Assert.DoesNotContain("Private student notes", rawStudent.Single().ProtectedNotes);
        Assert.DoesNotContain("456 Confidential Road", rawVisit.Single().ProtectedMeetingAddress);
        Assert.DoesNotContain("Private visit notes", rawVisit.Single().ProtectedNotes);

        var loadedStudent = await db.Service.GetStudentByIdAsync(student.StudentId);
        var loadedVisit = await db.Service.GetVisitByIdAsync(visit.Id);
        Assert.Equal(student.Name, loadedStudent?.Name);
        Assert.Equal(student.PhoneNumber, loadedStudent?.PhoneNumber);
        Assert.Equal(student.PrimaryLatitude, loadedStudent?.PrimaryLatitude);
        Assert.Equal(visit.MeetingAddress, loadedVisit?.MeetingAddress);
        Assert.Equal(visit.Notes, loadedVisit?.Notes);

        SQLiteAsyncConnection.ResetPool();
        var databaseText = System.Text.Encoding.UTF8.GetString(
            await File.ReadAllBytesAsync(db.Path));
        Assert.DoesNotContain(student.Name, databaseText);
        Assert.DoesNotContain(student.PhoneNumber, databaseText);
        Assert.DoesNotContain(student.Notes, databaseText);
        Assert.DoesNotContain(visit.MeetingAddress, databaseText);
        Assert.DoesNotContain(visit.Notes, databaseText);
    }

    [Fact]
    public async Task Reopening_with_the_wrong_key_is_rejected()
    {
        await using var db = await TestDatabase.CreateAsync();
        await db.AddStudentAsync(name: "Protected");
        SQLiteAsyncConnection.ResetPool();

        var wrongKeyService = TestDatabase.CreateService(
            db.Path,
            RandomNumberGenerator.GetBytes(32));

        await Assert.ThrowsAnyAsync<CryptographicException>(
            () => wrongKeyService.InitializeAsync());
    }

    private sealed class RawStudent
    {
        public string ProtectedName { get; set; } = string.Empty;
        public string ProtectedNotes { get; set; } = string.Empty;
    }

    private sealed class RawVisit
    {
        public string ProtectedMeetingAddress { get; set; } = string.Empty;
        public string ProtectedNotes { get; set; } = string.Empty;
    }
}
