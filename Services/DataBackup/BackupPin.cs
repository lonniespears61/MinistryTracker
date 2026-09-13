using System.Security.Cryptography;

namespace MinistryTracker.Services.Backup;

public static class BackupPin
{
    public const int Length = 6;

    public static string Create() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static bool IsValid(string? value) =>
        value is { Length: Length } && value.All(char.IsAsciiDigit);
}
