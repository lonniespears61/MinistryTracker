using System.Security.Cryptography;
using System.Text.Json;

namespace MinistryTracker.Services.Backup;

public sealed class BackupEncryptionService
{
    private const int BackupFormatVersion = 1;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 310_000;

    public BackupEnvelope EncryptPackage(BackupPlaintextPackage package, string backupCode)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (string.IsNullOrWhiteSpace(backupCode))
            throw new ArgumentException("Backup code is required.", nameof(backupCode));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            backupCode,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(
            package,
            BackupJsonContext.Default.BackupPlaintextPackage);

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(
            package.Metadata,
            BackupJsonContext.Default.BackupMetadata);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, metadataBytes);

        CryptographicOperations.ZeroMemory(key);

        return new BackupEnvelope
        {
            BackupFormatVersion = BackupFormatVersion,
            Metadata = package.Metadata,
            Kdf = new BackupKdfParameters
            {
                Iterations = Iterations
            },
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Ciphertext = Convert.ToBase64String(ciphertext),
            Tag = Convert.ToBase64String(tag)
        };
    }

    public BackupPlaintextPackage DecryptPackage(BackupEnvelope envelope, string backupCode)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(backupCode))
            throw new ArgumentException("Backup PIN is required.", nameof(backupCode));

        if (envelope.BackupFormatVersion != BackupFormatVersion ||
            envelope.Metadata.BackupFormatVersion != BackupFormatVersion)
        {
            throw new NotSupportedException("This backup format is not supported.");
        }

        if (!string.Equals(envelope.Kdf.Name, "PBKDF2-HMAC-SHA256", StringComparison.Ordinal) ||
            envelope.Kdf.Iterations != Iterations ||
            !string.Equals(envelope.Encryption, "AES-256-GCM", StringComparison.Ordinal))
        {
            throw new NotSupportedException("This backup encryption format is not supported.");
        }

        var salt = DecodeExactSize(envelope.Salt, SaltSize, "salt");
        var nonce = DecodeExactSize(envelope.Nonce, NonceSize, "nonce");
        var tag = DecodeExactSize(envelope.Tag, TagSize, "authentication tag");
        var ciphertext = DecodeBase64(envelope.Ciphertext, "encrypted data");
        var key = Rfc2898DeriveBytes.Pbkdf2(
            backupCode,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        try
        {
            var plaintext = new byte[ciphertext.Length];
            var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(
                envelope.Metadata,
                BackupJsonContext.Default.BackupMetadata);

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, metadataBytes);

            return JsonSerializer.Deserialize(
                       plaintext,
                       BackupJsonContext.Default.BackupPlaintextPackage)
                   ?? throw new InvalidDataException("The backup contains no data.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DecodeExactSize(string value, int expectedSize, string fieldName)
    {
        var bytes = DecodeBase64(value, fieldName);
        return bytes.Length == expectedSize
            ? bytes
            : throw new InvalidDataException($"The backup {fieldName} is invalid.");
    }

    private static byte[] DecodeBase64(string value, string fieldName)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException($"The backup {fieldName} is invalid.", ex);
        }
    }
}
