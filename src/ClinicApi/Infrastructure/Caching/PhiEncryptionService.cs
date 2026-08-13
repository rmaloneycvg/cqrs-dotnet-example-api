using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClinicApi.Infrastructure.Caching;

/// <summary>
/// Encrypts and decrypts PHI data using AES-256-GCM before storage in Redis.
///
/// Architecture:
///   - Key sourced from configuration (dev) or Vault sidecar (staging/prod)
///   - AES-256-GCM provides authenticated encryption (confidentiality + integrity)
///   - Each encryption operation uses a unique nonce (12 bytes)
///   - Output format: [nonce:12][tag:16][ciphertext:N] → base64 encoded
///
/// PHI fields (FirstName, LastName, Email, Phone, DateOfBirth, Address) are encrypted.
/// Non-PHI fields (Id, OrganizationId, Status, timestamps) remain plaintext for
/// subscription routing and cache key resolution.
/// </summary>
public interface IPhiEncryptionService
{
    /// <summary>Encrypt a PHI payload (serialized JSON of PHI fields) to a base64 string.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypt a base64-encoded payload back to plaintext JSON.</summary>
    string Decrypt(string ciphertext);

    /// <summary>Encrypt an object's PHI fields, returning the encrypted base64 blob.</summary>
    string EncryptObject<T>(T phiData);

    /// <summary>Decrypt a base64 blob back to the original PHI object.</summary>
    T DecryptObject<T>(string encryptedBase64);
}

public class AesGcmPhiEncryptionService : IPhiEncryptionService
{
    private readonly byte[] _key;

    // AES-GCM constants
    private const int NonceSize = 12; // 96 bits (recommended for AES-GCM)
    private const int TagSize = 16;   // 128 bits

    /// <summary>
    /// Create with a base64-encoded 256-bit (32-byte) key.
    /// In development: from appsettings/configuration.
    /// In production: from Vault sidecar injection.
    /// </summary>
    public AesGcmPhiEncryptionService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new ArgumentException("PHI encryption key must be exactly 32 bytes (256-bit).");
    }

    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Output: [nonce][tag][ciphertext]
        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSize);
        ciphertext.CopyTo(result, NonceSize + TagSize);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertextBase64)
    {
        var combined = Convert.FromBase64String(ciphertextBase64);

        if (combined.Length < NonceSize + TagSize)
            throw new CryptographicException("Invalid ciphertext: too short.");

        var nonce = combined[..NonceSize];
        var tag = combined[NonceSize..(NonceSize + TagSize)];
        var ciphertext = combined[(NonceSize + TagSize)..];

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public string EncryptObject<T>(T phiData)
    {
        var json = JsonSerializer.Serialize(phiData);
        return Encrypt(json);
    }

    public T DecryptObject<T>(string encryptedBase64)
    {
        var json = Decrypt(encryptedBase64);
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException("Failed to deserialize decrypted PHI data.");
    }
}
