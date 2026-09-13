using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MinistryTracker.Data;

namespace MinistryTracker.Services.Backup;

public sealed class BackupExportService
{
    public const int BackupFormatVersion = 1;

    private readonly DataService _data;
    private readonly BackupEncryptionService _encryption;

    public BackupExportService(DataService data, BackupEncryptionService encryption)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
    }

    public async Task<BackupExportResult> ExportAsync(string backupCode, CancellationToken ct = default)
    {
        var students = await _data.GetStudentsAsync(includeDeleted: true, ct).ConfigureAwait(false);
        var visits = new List<BackupVisitDto>();

        foreach (var student in students)
        {
            ct.ThrowIfCancellationRequested();
            var studentVisits = await _data.GetVisitsForStudentAsync(student.StudentId, ct).ConfigureAwait(false);
            visits.AddRange(studentVisits.Select(visit => new BackupVisitDto
            {
                Id = visit.Id,
                StudentId = visit.StudentId,
                Method = visit.Method,
                ScheduledDateTime = visit.ScheduledDateTime,
                Status = visit.Status,
                CompletedDateTime = visit.CompletedDateTime,
                MeetingAddress = visit.MeetingAddress,
                MeetingLatitude = visit.MeetingLatitude,
                MeetingLongitude = visit.MeetingLongitude,
                RescheduledFromVisitId = visit.RescheduledFromVisitId,
                Notes = visit.Notes,
                NotesCreatedDateTime = visit.NotesCreatedDateTime
            }));
        }

        visits = visits
            .OrderBy(visit => visit.StudentId)
            .ThenBy(visit => visit.ScheduledDateTime)
            .ThenBy(visit => visit.Id)
            .ToList();

        var package = new BackupPlaintextPackage
        {
            Metadata = new BackupMetadata
            {
                BackupFormatVersion = BackupFormatVersion,
                CreatedUtc = DateTime.UtcNow,
                AppVersion = AppInfo.Current.VersionString,
                AppBuild = AppInfo.Current.BuildString,
                AppSchemaVersion = _data.GetAppSchemaVersion(),
                DatabaseSchemaVersion = await _data.GetDatabaseSchemaVersionAsync().ConfigureAwait(false),
                StudentCount = students.Count,
                VisitCount = visits.Count
            },
            Students = students.Select(student => new BackupStudentDto
            {
                StudentId = student.StudentId,
                Name = student.Name,
                InitialContactType = student.InitialContactType,
                FirstContactDate = student.FirstContactDate,
                PhoneNumber = student.PhoneNumber,
                Email = student.Email,
                PrimaryAddress = student.PrimaryAddress,
                IsHomeAddress = student.IsHomeAddress,
                LocationContext = student.LocationContext,
                PrimaryLatitude = student.PrimaryLatitude,
                PrimaryLongitude = student.PrimaryLongitude,
                PrimaryGeocodeStatus = student.PrimaryGeocodeStatus,
                PreferredLanguage = student.PreferredLanguage,
                PreferredContactMethod = student.PreferredContactMethod,
                Status = student.Status,
                Gender = student.Gender,
                Age = student.Age,
                Notes = student.Notes,
                IsDeleted = student.IsDeleted
            }).ToList(),
            Visits = visits
        };

        var envelope = _encryption.EncryptPackage(package, backupCode);
        var fileName = $"ministrytracker-backup-{package.Metadata.CreatedUtc:yyyyMMdd-HHmmss}.mtbackup";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        var json = JsonSerializer.Serialize(envelope, BackupJsonContext.Default.BackupEnvelope);

        await File.WriteAllTextAsync(filePath, json, ct).ConfigureAwait(false);

        return new BackupExportResult(
            filePath,
            fileName,
            package.Metadata.StudentCount,
            package.Metadata.VisitCount,
            package.Metadata.CreatedUtc);
    }
}

public sealed record BackupExportResult(
    string FilePath,
    string FileName,
    int StudentCount,
    int VisitCount,
    DateTime CreatedUtc);
