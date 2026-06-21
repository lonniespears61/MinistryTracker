using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using SQLite;

namespace MinistryTracker.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly string _directory;

    private TestDatabase(string directory, DataService service)
    {
        _directory = directory;
        Service = service;
    }

    public DataService Service { get; }
    public string Path => Service.GetDatabasePath();

    public static async Task<TestDatabase> CreateAsync()
    {
        var directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MinistryTracker.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        var service = new DataService(
            System.IO.Path.Combine(directory, "ministrytracker.db3"));

        await service.InitializeAsync();
        return new TestDatabase(directory, service);
    }

    public async Task<Student> AddStudentAsync(
        StudentStatus status = StudentStatus.Active,
        bool isDeleted = false,
        string? name = null)
    {
        var student = new Student
        {
            Name = name ?? status.ToString(),
            Status = status,
            IsDeleted = isDeleted,
            FirstContactDate = DateTime.Today
        };

        await Service.AddStudentAsync(student);
        return student;
    }

    public static Visit NewVisit(int studentId, DateTime when) => new()
    {
        StudentId = studentId,
        Method = ContactMethod.InPerson,
        ScheduledDateTime = when,
        Status = VisitStatus.Scheduled
    };

    public SQLiteAsyncConnection OpenRaw() => new(Path);

    public async ValueTask DisposeAsync()
    {
        SQLiteAsyncConnection.ResetPool();
        await Task.Yield();

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Windows may briefly retain SQLite handles after a test.
        }
    }
}
