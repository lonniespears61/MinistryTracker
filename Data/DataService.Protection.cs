using System.Globalization;
using System.Security.Cryptography;
using MinistryTracker.Models;
using SQLite;

namespace MinistryTracker.Data;

public partial class DataService
{
    private const string VerificationText = "ministrytoolworks-data-key-v1";

    private void ProtectForWrite(Student student)
    {
        student.ProtectedName = _dataProtection.Protect(student.Name);
        student.ProtectedPhoneNumber = _dataProtection.Protect(student.PhoneNumber);
        student.ProtectedEmail = _dataProtection.Protect(student.Email);
        student.ProtectedPrimaryAddress = _dataProtection.Protect(student.PrimaryAddress);
        student.ProtectedPrimaryLatitude = ProtectNullable(student.PrimaryLatitude);
        student.ProtectedPrimaryLongitude = ProtectNullable(student.PrimaryLongitude);
        student.ProtectedPreferredLanguage = _dataProtection.Protect(student.PreferredLanguage);
        student.ProtectedGender = _dataProtection.Protect(
            student.Gender?.ToString());
        student.ProtectedAge = _dataProtection.Protect(
            student.Age?.ToString(CultureInfo.InvariantCulture));
        student.ProtectedNotes = _dataProtection.Protect(student.Notes);
        student.ProtectionVersion = 1;
    }

    private void UnprotectAfterRead(Student student)
    {
        student.Name = _dataProtection.Unprotect(student.ProtectedName) ?? string.Empty;
        student.PhoneNumber = _dataProtection.Unprotect(student.ProtectedPhoneNumber);
        student.Email = _dataProtection.Unprotect(student.ProtectedEmail);
        student.PrimaryAddress = _dataProtection.Unprotect(student.ProtectedPrimaryAddress);
        student.PrimaryLatitude = UnprotectDouble(student.ProtectedPrimaryLatitude);
        student.PrimaryLongitude = UnprotectDouble(student.ProtectedPrimaryLongitude);
        student.PreferredLanguage = _dataProtection.Unprotect(student.ProtectedPreferredLanguage);

        var gender = _dataProtection.Unprotect(student.ProtectedGender);
        student.Gender = Enum.TryParse<Models.Gender>(gender, out var parsedGender)
            ? parsedGender
            : null;

        var age = _dataProtection.Unprotect(student.ProtectedAge);
        student.Age = int.TryParse(age, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAge)
            ? parsedAge
            : null;

        student.Notes = _dataProtection.Unprotect(student.ProtectedNotes);
    }

    private void ProtectForWrite(Visit visit)
    {
        visit.ProtectedMeetingAddress = _dataProtection.Protect(visit.MeetingAddress);
        visit.ProtectedMeetingLatitude = ProtectNullable(visit.MeetingLatitude);
        visit.ProtectedMeetingLongitude = ProtectNullable(visit.MeetingLongitude);
        visit.ProtectedNotes = _dataProtection.Protect(visit.Notes);
        visit.ProtectionVersion = 1;
    }

    private void UnprotectAfterRead(Visit visit)
    {
        visit.MeetingAddress = _dataProtection.Unprotect(visit.ProtectedMeetingAddress);
        visit.MeetingLatitude = UnprotectDouble(visit.ProtectedMeetingLatitude);
        visit.MeetingLongitude = UnprotectDouble(visit.ProtectedMeetingLongitude);
        visit.Notes = _dataProtection.Unprotect(visit.ProtectedNotes);
    }

    private string? ProtectNullable(double? value) =>
        _dataProtection.Protect(value?.ToString("R", CultureInfo.InvariantCulture));

    private double? UnprotectDouble(string? value)
    {
        var plaintext = _dataProtection.Unprotect(value);
        return double.TryParse(
            plaintext,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }

    private List<Student> UnprotectStudents(List<Student> students)
    {
        foreach (var student in students)
            UnprotectAfterRead(student);

        return students;
    }

    private List<Visit> UnprotectVisits(List<Visit> visits)
    {
        foreach (var visit in visits)
            UnprotectAfterRead(visit);

        return visits;
    }

    private async Task VerifyDataProtectionKeyAsync(SQLiteAsyncConnection db)
    {
        var metadata = await db.FindAsync<DataProtectionMetadata>(1).ConfigureAwait(false);
        if (metadata is null)
        {
            await db.InsertAsync(new DataProtectionMetadata
            {
                Id = 1,
                ProtectedVerificationValue = _dataProtection.Protect(VerificationText)
                    ?? throw new CryptographicException("Could not create the data-key verifier.")
            }).ConfigureAwait(false);
            return;
        }

        var verification = _dataProtection.Unprotect(metadata.ProtectedVerificationValue);
        if (!string.Equals(verification, VerificationText, StringComparison.Ordinal))
            throw new CryptographicException("The device data-encryption key does not match this database.");
    }

    private async Task MigrateLegacyPlaintextAsync(SQLiteAsyncConnection db)
    {
        var studentColumns = await db.GetTableInfoAsync("Students").ConfigureAwait(false);
        if (studentColumns.Any(column => column.Name == "Name"))
        {
            var students = await db.QueryAsync<LegacyStudent>(
                """
                SELECT StudentId, Name, PhoneNumber, Email, PrimaryAddress,
                       PrimaryLatitude, PrimaryLongitude, PreferredLanguage,
                       Gender, Age, Notes
                FROM Students
                WHERE ProtectionVersion < 1
                """).ConfigureAwait(false);

            foreach (var legacy in students)
            {
                var student = new Student
                {
                    Name = legacy.Name ?? string.Empty,
                    PhoneNumber = legacy.PhoneNumber,
                    Email = legacy.Email,
                    PrimaryAddress = legacy.PrimaryAddress,
                    PrimaryLatitude = legacy.PrimaryLatitude,
                    PrimaryLongitude = legacy.PrimaryLongitude,
                    PreferredLanguage = legacy.PreferredLanguage,
                    Gender = legacy.Gender,
                    Age = legacy.Age,
                    Notes = legacy.Notes
                };
                ProtectForWrite(student);

                await db.ExecuteAsync(
                    """
                    UPDATE Students
                    SET ProtectedName = ?, ProtectedPhoneNumber = ?, ProtectedEmail = ?,
                        ProtectedPrimaryAddress = ?, ProtectedPrimaryLatitude = ?,
                        ProtectedPrimaryLongitude = ?, ProtectedPreferredLanguage = ?,
                        ProtectedGender = ?, ProtectedAge = ?, ProtectedNotes = ?,
                        Name = '', PhoneNumber = NULL, Email = NULL, PrimaryAddress = NULL,
                        PrimaryLatitude = NULL, PrimaryLongitude = NULL, PreferredLanguage = NULL,
                        Gender = NULL, Age = NULL, Notes = NULL,
                        ProtectionVersion = 1
                    WHERE StudentId = ?
                    """,
                    student.ProtectedName,
                    student.ProtectedPhoneNumber,
                    student.ProtectedEmail,
                    student.ProtectedPrimaryAddress,
                    student.ProtectedPrimaryLatitude,
                    student.ProtectedPrimaryLongitude,
                    student.ProtectedPreferredLanguage,
                    student.ProtectedGender,
                    student.ProtectedAge,
                    student.ProtectedNotes,
                    legacy.StudentId).ConfigureAwait(false);
            }
        }

        var visitColumns = await db.GetTableInfoAsync("Visits").ConfigureAwait(false);
        if (visitColumns.Any(column => column.Name == "MeetingAddress"))
        {
            var visits = await db.QueryAsync<LegacyVisit>(
                """
                SELECT Id, MeetingAddress, MeetingLatitude, MeetingLongitude, Notes
                FROM Visits
                WHERE ProtectionVersion < 1
                """).ConfigureAwait(false);

            foreach (var legacy in visits)
            {
                var visit = new Visit
                {
                    MeetingAddress = legacy.MeetingAddress,
                    MeetingLatitude = legacy.MeetingLatitude,
                    MeetingLongitude = legacy.MeetingLongitude,
                    Notes = legacy.Notes
                };
                ProtectForWrite(visit);

                await db.ExecuteAsync(
                    """
                    UPDATE Visits
                    SET ProtectedMeetingAddress = ?, ProtectedMeetingLatitude = ?,
                        ProtectedMeetingLongitude = ?, ProtectedNotes = ?,
                        MeetingAddress = NULL, MeetingLatitude = NULL,
                        MeetingLongitude = NULL, Notes = NULL,
                        ProtectionVersion = 1
                    WHERE Id = ?
                    """,
                    visit.ProtectedMeetingAddress,
                    visit.ProtectedMeetingLatitude,
                    visit.ProtectedMeetingLongitude,
                    visit.ProtectedNotes,
                    legacy.Id).ConfigureAwait(false);
            }
        }

        await db.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE);").ConfigureAwait(false);
        await db.ExecuteAsync("VACUUM;").ConfigureAwait(false);
    }

    private sealed class LegacyStudent
    {
        public int StudentId { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? PrimaryAddress { get; set; }
        public double? PrimaryLatitude { get; set; }
        public double? PrimaryLongitude { get; set; }
        public string? PreferredLanguage { get; set; }
        public Models.Gender? Gender { get; set; }
        public int? Age { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class LegacyVisit
    {
        public int Id { get; set; }
        public string? MeetingAddress { get; set; }
        public double? MeetingLatitude { get; set; }
        public double? MeetingLongitude { get; set; }
        public string? Notes { get; set; }
    }
}
