using System.Text.Json.Serialization;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Services.Backup;

public sealed class BackupPlaintextPackage
{
    public BackupMetadata Metadata { get; set; } = new();
    public List<BackupStudentDto> Students { get; set; } = new();
    public List<BackupVisitDto> Visits { get; set; } = new();
}

public sealed class BackupEnvelope
{
    public int BackupFormatVersion { get; set; }
    public BackupMetadata Metadata { get; set; } = new();
    public BackupKdfParameters Kdf { get; set; } = new();
    public string Encryption { get; set; } = "AES-256-GCM";
    public string Salt { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Ciphertext { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
}

public sealed class BackupMetadata
{
    public int BackupFormatVersion { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string? AppVersion { get; set; }
    public string? AppBuild { get; set; }
    public int? AppSchemaVersion { get; set; }
    public int? DatabaseSchemaVersion { get; set; }
    public int StudentCount { get; set; }
    public int VisitCount { get; set; }
}

public sealed class BackupKdfParameters
{
    public string Name { get; set; } = "PBKDF2-HMAC-SHA256";
    public int Iterations { get; set; }
}

public sealed class BackupStudentDto
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public InitialContactType InitialContactType { get; set; }
    public DateTime FirstContactDate { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? PrimaryAddress { get; set; }
    public bool IsHomeAddress { get; set; }
    public LocationContext LocationContext { get; set; }
    public double? PrimaryLatitude { get; set; }
    public double? PrimaryLongitude { get; set; }
    public GeocodeStatus PrimaryGeocodeStatus { get; set; }
    public string? PreferredLanguage { get; set; }
    public ContactMethod? PreferredContactMethod { get; set; }
    public StudentStatus Status { get; set; }
    public Gender? Gender { get; set; }
    public int? Age { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class BackupVisitDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public ContactMethod Method { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public VisitStatus Status { get; set; }
    public DateTime? CompletedDateTime { get; set; }
    public string? MeetingAddress { get; set; }
    public double? MeetingLatitude { get; set; }
    public double? MeetingLongitude { get; set; }
    public int? RescheduledFromVisitId { get; set; }
    public string? Notes { get; set; }
    public DateTime? NotesCreatedDateTime { get; set; }
}

[JsonSerializable(typeof(BackupPlaintextPackage))]
[JsonSerializable(typeof(BackupEnvelope))]
[JsonSerializable(typeof(BackupMetadata))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public sealed partial class BackupJsonContext : JsonSerializerContext
{
}
