using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BeSwarm.CoreWebApi.Services.Crypto;
//
// Cipher service
//
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string cyphertext);
    public string Name { get; }
}
public class AesEncryptionService : IEncryptionService
{
    public string Name => "Aes";
    private readonly IEncryptionKeyProvider encryptionKeyProvider;

    public AesEncryptionService(IEncryptionKeyProvider encryptionKeyProvider)
    {
        this.encryptionKeyProvider = encryptionKeyProvider;
    }

    public string Encrypt(string plaintext)
    {
        byte[] cyphertextBytes;
        var encryptionKey = encryptionKeyProvider.GetCurrentEncryptionKey();
        using var aes = Aes.Create();
        var encryptor = aes.CreateEncryptor(encryptionKey.key, aes.IV);
        using (var memoryStream = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                using (var streamWriter = new StreamWriter(cryptoStream))
                {
                    streamWriter.Write(plaintext);
                }
            }
            cyphertextBytes = memoryStream.ToArray();

            return new AesCbcCiphertext(Name, encryptionKey.version, aes.IV, cyphertextBytes).ToString();
        }
    }

    public string Decrypt(string ciphertext)
    {
        var cbcCiphertext = AesCbcCiphertext.FromBase64String(ciphertext);
        if(cbcCiphertext.Name!=Name)
        { return Encoding.UTF8.GetString(cbcCiphertext.CiphertextBytes);
        }
        using var aes = Aes.Create();
        var encryptionKey = encryptionKeyProvider.GetEncryptionKeyById(cbcCiphertext.Key);
        var decryptor = aes.CreateDecryptor(encryptionKey.key, cbcCiphertext.Iv);
        using (var memoryStream = new MemoryStream(cbcCiphertext.CiphertextBytes))
        {
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            {
                using (var streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
public class AesCbcCiphertext
{
    public byte[] Iv { get; }
    public byte[] CiphertextBytes { get; }
    public string Key { get; }
    public string Name { get; }


    public static AesCbcCiphertext FromBase64String(string data)
    {
        var parts = data.Split('$');
        if(parts.Length<2)
        {
            return new("none", "", null, Encoding.UTF8.GetBytes(data));
        }
        var dataBytes = Convert.FromBase64String(parts[2]);
        return new AesCbcCiphertext(parts[0], parts[1],
            dataBytes.Take(16).ToArray(),
            dataBytes.Skip(16).ToArray()
        );
    }

    public AesCbcCiphertext(string name, string key, byte[] iv, byte[] ciphertextBytes)
    {
        Iv = iv;
        CiphertextBytes = ciphertextBytes;
        Key = key;
        Name = name;
    }

    public override string ToString()
    {
        return $"{Name}${Key}${Convert.ToBase64String(Iv.Concat(CiphertextBytes).ToArray())}";
    }
}



//
//
// Rotate Key provider
//
//
public record EncryptionKey(string version, byte[] key);

public interface IEncryptionKeyProvider
{
    public EncryptionKey GetCurrentEncryptionKey();
    public EncryptionKey GetEncryptionKeyById(string keyId);
    public void RotateKey(string newKeyData);
}

public class EncryptionKeyProvider : IEncryptionKeyProvider
{
    private readonly List<byte[]> encryptionKeys = new();

    public EncryptionKeyProvider()
    {
    }

    public EncryptionKey GetCurrentEncryptionKey()
    {
        return new EncryptionKey($"v{encryptionKeys.Count - 1}", encryptionKeys.Last());
    }

    public EncryptionKey GetEncryptionKeyById(string keyId)
    {
        var keyIndex = int.Parse(keyId[1..]);

        return new EncryptionKey(keyId, encryptionKeys[keyIndex]);
    }

    public void RotateKey(string newKeyData)
    {
        if (newKeyData.Length != 32) throw new Exception("key must be 32 bytes length");
        encryptionKeys.Add(Encoding.UTF8.GetBytes(newKeyData));
    }
}
