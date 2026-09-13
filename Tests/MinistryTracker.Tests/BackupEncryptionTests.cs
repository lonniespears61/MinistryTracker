using System.Security.Cryptography;
using MinistryTracker.Services.Backup;

namespace MinistryTracker.Tests;

public sealed class BackupEncryptionTests
{
    [Fact]
    public void BackupPin_AcceptsExactlySixAsciiDigits()
    {
        Assert.True(BackupPin.IsValid("012345"));
        Assert.False(BackupPin.IsValid("12345"));
        Assert.False(BackupPin.IsValid("1234567"));
        Assert.False(BackupPin.IsValid("12345A"));
        Assert.False(BackupPin.IsValid("１２３４５６"));
    }

    [Fact]
    public void BackupPin_CreateProducesValidPins()
    {
        for (var index = 0; index < 100; index++)
            Assert.True(BackupPin.IsValid(BackupPin.Create()));
    }

    [Fact]
    public void Encryption_RoundTripsPackage()
    {
        var service = new BackupEncryptionService();
        var package = CreatePackage();

        var envelope = service.EncryptPackage(package, "042719");
        var restored = service.DecryptPackage(envelope, "042719");

        Assert.Equal(package.Metadata.CreatedUtc, restored.Metadata.CreatedUtc);
        Assert.Equal("Test Student", Assert.Single(restored.Students).Name);
        Assert.Equal("Visit note", Assert.Single(restored.Visits).Notes);
    }

    [Fact]
    public void Encryption_RejectsWrongPin()
    {
        var service = new BackupEncryptionService();
        var envelope = service.EncryptPackage(CreatePackage(), "042719");

        Assert.Throws<AuthenticationTagMismatchException>(
            () => service.DecryptPackage(envelope, "999999"));
    }

    [Fact]
    public void Encryption_RejectsChangedMetadata()
    {
        var service = new BackupEncryptionService();
        var envelope = service.EncryptPackage(CreatePackage(), "042719");
        envelope.Metadata.StudentCount++;

        Assert.Throws<AuthenticationTagMismatchException>(
            () => service.DecryptPackage(envelope, "042719"));
    }

    private static BackupPlaintextPackage CreatePackage()
    {
        var createdUtc = new DateTime(2026, 8, 26, 12, 30, 0, DateTimeKind.Utc);
        return new BackupPlaintextPackage
        {
            Metadata = new BackupMetadata
            {
                BackupFormatVersion = 1,
                CreatedUtc = createdUtc,
                StudentCount = 1,
                VisitCount = 1
            },
            Students =
            {
                new BackupStudentDto
                {
                    StudentId = 1,
                    Name = "Test Student"
                }
            },
            Visits =
            {
                new BackupVisitDto
                {
                    Id = 10,
                    StudentId = 1,
                    ScheduledDateTime = createdUtc,
                    Notes = "Visit note"
                }
            }
        };
    }
}
