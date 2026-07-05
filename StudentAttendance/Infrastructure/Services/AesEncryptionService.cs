using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StudentAttendance.Core.Interfaces;

namespace StudentAttendance.Infrastructure.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration configuration)
    {
        // Read keys from appsettings.json. In production, use Azure Key Vault or Environment Variables.
        // Fallbacks provided for demonstration if config is missing.
        var keyString = configuration["EncryptionSettings:Key"] ?? "b14ca5898a4e4133bbce2ea2315a1916"; // 32 chars for AES-256
        var ivString = configuration["EncryptionSettings:IV"] ?? "f38a543210987654"; // 16 chars

        _key = Encoding.UTF8.GetBytes(keyString);
        _iv = Encoding.UTF8.GetBytes(ivString);
    }

    public byte[] Encrypt(byte[] plainData)
    {
        if (plainData == null || plainData.Length == 0) return Array.Empty<byte>();

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        
        cs.Write(plainData, 0, plainData.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0) return Array.Empty<byte>();

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var outputMs = new MemoryStream();
        
        cs.CopyTo(outputMs);
        return outputMs.ToArray();
    }

    public string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = Encrypt(bytes);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string DecryptString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;
        var bytes = Convert.FromBase64String(encryptedText);
        var decryptedBytes = Decrypt(bytes);
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
