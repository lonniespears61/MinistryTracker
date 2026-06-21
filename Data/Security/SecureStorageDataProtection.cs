using System.Security.Cryptography;
using Microsoft.Maui.Storage;

namespace MinistryTracker.Data.Security;

public static class SecureStorageDataProtection
{
    private const string KeyName = "ministrytoolworks.data-key.v1";

    public static IDataProtectionService Create() =>
        new AesGcmDataProtectionService(LoadOrCreateKeyAsync);

    private static async Task<byte[]> LoadOrCreateKeyAsync()
    {
        var existing = await SecureStorage.Default.GetAsync(KeyName).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(existing))
            return Convert.FromBase64String(existing);

        var key = RandomNumberGenerator.GetBytes(32);
        await SecureStorage.Default.SetAsync(KeyName, Convert.ToBase64String(key)).ConfigureAwait(false);

        var verification = await SecureStorage.Default.GetAsync(KeyName).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(verification))
            throw new InvalidOperationException("The device could not securely retain the data-encryption key.");

        return Convert.FromBase64String(verification);
    }
}
