using System.Security.Cryptography;
using System.Text;

namespace OneManVan.Shared.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data using AES encryption.
/// Use this for PII fields like SSN, EIN, bank account numbers, etc.
/// This implementation doesn't require ASP.NET Core dependencies.
/// </summary>
public interface IDataProtectionService
{
    /// <summary>
    /// Encrypts sensitive data.
    /// </summary>
    string Encrypt(string? plainText);
    
    /// <summary>
    /// Decrypts protected data.
    /// </summary>
    string Decrypt(string? protectedData);
    
    /// <summary>
    /// Attempts to decrypt data. Returns original value if decryption fails.
    /// Use this for backward compatibility with unencrypted data.
    /// </summary>
    string TryDecrypt(string? data);
}

public class DataProtectionService : IDataProtectionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    // Default key derivation - in production, use a secure key from configuration
    private const string DefaultKeyMaterial = "OneManVan-Secure-Data-Protection-Key-v1";

    public DataProtectionService()
    {
        // Derive key and IV from key material using SHA256
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(DefaultKeyMaterial));
        _iv = sha256.ComputeHash(Encoding.UTF8.GetBytes(DefaultKeyMaterial + "-IV")).Take(16).ToArray();
    }
    
    public DataProtectionService(string keyMaterial)
    {
        // Derive key and IV from provided key material
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial));
        _iv = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial + "-IV")).Take(16).ToArray();
    }

    public string Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string? protectedData)
    {
        if (string.IsNullOrEmpty(protectedData))
            return string.Empty;

        var buffer = Convert.FromBase64String(protectedData);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }

    public string TryDecrypt(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;
        
        try
        {
            // Try to decrypt - if it fails, the data might not be encrypted yet
            return Decrypt(data);
        }
        catch (Exception)
        {
            // Return original value if decryption fails (backward compatibility)
            return data;
        }
    }
}
