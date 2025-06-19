using System;
using System.Security.Cryptography;
using System.Text;

public class HybridEncryptionHelper
{
    public class DualEncryptionResult
    {
        public string EncryptedMessage { get; set; }
        public string EncryptedKeyForSender { get; set; }
        public string EncryptedKeyForReceiver { get; set; }
        public string IV { get; set; }
    }

    public static DualEncryptionResult EncryptForBoth(string plainText, string senderPublicKey, string receiverPublicKey)
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        byte[] encryptedKeySender, encryptedKeyReceiver;

        using (var rsaSender = RSA.Create())
        {
            ImportPublicKey(rsaSender, senderPublicKey);
            encryptedKeySender = rsaSender.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
        }

        using (var rsaReceiver = RSA.Create())
        {
            ImportPublicKey(rsaReceiver, receiverPublicKey);
            encryptedKeyReceiver = rsaReceiver.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
        }

        return new DualEncryptionResult
        {
            EncryptedMessage = Convert.ToBase64String(encryptedBytes),
            EncryptedKeyForSender = Convert.ToBase64String(encryptedKeySender),
            EncryptedKeyForReceiver = Convert.ToBase64String(encryptedKeyReceiver),
            IV = Convert.ToBase64String(aes.IV)
        };
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
        {
            rsa.ImportFromPem(publicKey.ToCharArray());
        }
        else
        {
            var publicKeyBytes = Convert.FromBase64String(publicKey);
            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
        }
    }

    private static void ImportPrivateKey(RSA rsa, string privateKey)
    {
        if (privateKey.StartsWith("-----BEGIN"))
        {
            rsa.ImportFromPem(privateKey.ToCharArray());
        }
        else
        {
            var privateKeyBytes = Convert.FromBase64String(privateKey);
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
        }
    }
}