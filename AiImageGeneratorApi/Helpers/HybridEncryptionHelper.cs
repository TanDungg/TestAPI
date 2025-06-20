using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public class HybridEncryptionHelper
{
    public class MultiEncryptionResult
    {
        public string EncryptedMessage { get; set; }
        public Dictionary<Guid, string> EncryptedKeysPerUser { get; set; } = new();
        public string IV { get; set; }
    }

    public class DualEncryptionResult
    {
        public string EncryptedMessage { get; set; }
        public string IV { get; set; }
        public byte[] AESKey { get; set; } // Dùng để mã hóa riêng cho từng người
    }

    public static MultiEncryptionResult EncryptForMultiple(string plainText, Dictionary<Guid, string> publicKeysPerUser)
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new MultiEncryptionResult
        {
            EncryptedMessage = Convert.ToBase64String(encryptedBytes),
            IV = Convert.ToBase64String(aes.IV)
        };

        foreach (var kvp in publicKeysPerUser)
        {
            using var rsa = RSA.Create();
            ImportPublicKey(rsa, kvp.Value);
            var encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
            result.EncryptedKeysPerUser[kvp.Key] = Convert.ToBase64String(encryptedKey);
        }

        return result;
    }

    public static string Decrypt(string encryptedMessage, string encryptedKey, string iv, string privateKey)
    {
        var encryptedKeyBytes = Convert.FromBase64String(encryptedKey);
        var encryptedMessageBytes = Convert.FromBase64String(encryptedMessage);
        var ivBytes = Convert.FromBase64String(iv);

        using var rsa = RSA.Create();
        ImportPrivateKey(rsa, privateKey);
        var aesKey = rsa.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA256);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = ivBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedMessageBytes, 0, encryptedMessageBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static void ImportPublicKey(RSA rsa, string publicKey)
    {
        if (publicKey.StartsWith("-----BEGIN"))
            rsa.ImportFromPem(publicKey.ToCharArray());
        else
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
    }

    private static void ImportPrivateKey(RSA rsa, string privateKey)
    {
        if (privateKey.StartsWith("-----BEGIN"))
            rsa.ImportFromPem(privateKey.ToCharArray());
        else
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
    }

    public static DualEncryptionResult Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return new DualEncryptionResult
        {
            EncryptedMessage = Convert.ToBase64String(encryptedBytes),
            IV = Convert.ToBase64String(aes.IV),
            AESKey = aes.Key
        };
    }
    public static string EncryptAESKeyForUser(byte[] aesKey, string userPublicKey)
    {
        using var rsa = RSA.Create();
        ImportPublicKey(rsa, userPublicKey);
        return Convert.ToBase64String(rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256));
    }
}
