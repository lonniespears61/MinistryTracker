using System.Security.Cryptography;
using System.Text;

namespace MinistryTracker.Data.Security;

public sealed class AesGcmDataProtectionService : IDataProtectionService
{
    private const string Prefix = "mtw1:";
    private readonly Func<Task<byte[]>> _keyLoader;
    private byte[]? _key;

    public AesGcmDataProtectionService(Func<Task<byte[]>> keyLoader)
    {
        _keyLoader = keyLoader ?? throw new ArgumentNullException(nameof(keyLoader));
    }

    public async Task InitializeAsync()
    {
        if (_key is not null)
            return;

        var key = await _keyLoader().ConfigureAwait(false);
        if (key.Length != 32)
            throw new InvalidOperationException("The data-encryption key must be 256 bits.");

        _key = key.ToArray();
    }

    public string? Protect(string? plaintext)
    {
        if (plaintext is null)
            return null;

        var key = _key ?? throw new InvalidOperationException("Data protection is not initialized.");
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);
        return Prefix + Convert.ToBase64String(payload);
    }

    public string? Unprotect(string? protectedValue)
    {
        if (protectedValue is null)
            return null;

        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("The stored value is not in a recognized encrypted format.");

        var key = _key ?? throw new InvalidOperationException("Data protection is not initialized.");
        var payload = Convert.FromBase64String(protectedValue[Prefix.Length..]);
        if (payload.Length < 28)
            throw new CryptographicException("The encrypted value is incomplete.");

        var nonce = payload.AsSpan(0, 12);
        var tag = payload.AsSpan(12, 16);
        var ciphertext = payload.AsSpan(28);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
