using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClinicApi.Realtime.Infrastructure;

/// <summary>
/// PHI encryption for the Realtime service.
/// Uses the same AES-256-GCM algorithm as the Clinic API to decrypt cached PHI data.
/// Key must match the Clinic API's PHI_ENCRYPTION_KEY.
/// </summary>
public interface IPhiEncryptionService
{
    string Decrypt(string ciphertextBase64);
    T DecryptObject<T>(string encryptedBase64);
}

public class AesGcmPhiEncryptionService : IPhiEncryptionService
{
    private readonly byte[] _key;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public AesGcmPhiEncryptionService(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new ArgumentException("PHI encryption key must be exactly 32 bytes (256-bit).");
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

    public T DecryptObject<T>(string encryptedBase64)
    {
        var json = Decrypt(encryptedBase64);
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException("Failed to deserialize decrypted PHI data.");
    }
}
