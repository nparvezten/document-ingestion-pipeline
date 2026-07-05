using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace DocumentIngestion.Api.Services;

public class SecurityHelper
{
    private readonly byte[] _aesKey;
    private readonly byte[] _hmacKey;

    public SecurityHelper(IConfiguration configuration)
    {
        // AES key and HMAC key should be 32 bytes (256 bits) hex strings.
        // Falls back to stable testing keys if not configured in settings.
        string aesKeyString = configuration["Security:AesKey"] ?? "3b82f6a78bfa10b981f3f4f69ca3af093b82f6a78bfa10b981f3f4f69ca3af09"; 
        string hmacKeyString = configuration["Security:HmacKey"] ?? "8b5cf63b82f6a78bfa10b981f3f4f69ca3af093b82f6a78bfa10b981f3f4f69ca3"; 

        _aesKey = ConvertHexToBytes(aesKeyString);
        _hmacKey = ConvertHexToBytes(hmacKeyString);
    }

    public SecurityHelper(string aesHex, string hmacHex)
    {
        _aesKey = ConvertHexToBytes(aesHex);
        _hmacKey = ConvertHexToBytes(hmacHex);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _aesKey;
        
        // Generate random initialization vector (IV) for semantic security
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        
        // Prepend IV to the stream so it is stored alongside the ciphertext
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return string.Empty;

        try
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            if (fullCipher.Length < 16)
                return cipherText; // Fallback if data is too small to contain IV

            using var aes = Aes.Create();
            aes.Key = _aesKey;

            // Extract the 16-byte IV from the front
            byte[] iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract actual cipher bytes
            int cipherSize = fullCipher.Length - iv.Length;
            byte[] cipherBytes = new byte[cipherSize];
            Array.Copy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }
        catch
        {
            // Graceful fallback: return the raw cipher text to avoid crash
            return cipherText;
        }
    }

    public string ComputeBlindIndex(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Normalize text (trim, strip spaces, lowercase) to ensure search matches are deterministic
        string normalized = input.Trim().Replace(" ", "").ToLowerInvariant();
        byte[] bytes = Encoding.UTF8.GetBytes(normalized);

        using var hmac = new HMACSHA256(_hmacKey);
        byte[] hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static byte[] ConvertHexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hexadecimal key string must have an even number of characters.");

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}
