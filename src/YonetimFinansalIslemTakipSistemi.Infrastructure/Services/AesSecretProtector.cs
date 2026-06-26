using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// AES-256-CBC ile şifreleme. Anahtar appsettings "Encryption:SecretKey" değerinden türetilir.
/// Tüm Windows kullanıcıları ve tüm makineler aynı anahtarı kullandığından
/// şifreli değer PostgreSQL'de tüm istemciler tarafından çözülebilir.
/// IV (16 byte) şifreli verinin önüne eklenir; bütün değer Base64 olarak saklanır.
/// </summary>
public class AesSecretProtector : ISecretProtector
{
    private readonly byte[] _key;

    public AesSecretProtector(IConfiguration configuration)
    {
        // SecretKey Base64 veya düz metin olabilir; SHA-256 ile her zaman 32 byte'a normalize edilir
        var raw = configuration["Encryption:SecretKey"]
                  ?? "YonetimSistemiDefaultKey2025!Fallback";
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    public string Protect(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key  = _key;
        aes.Mode = CipherMode.CBC;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var data      = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

        // IV + ciphertext → Base64
        var result = new byte[aes.IV.Length + encrypted.Length];
        aes.IV.CopyTo(result, 0);
        encrypted.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public string Unprotect(string ciphertext)
    {
        var raw = Convert.FromBase64String(ciphertext);

        using var aes = Aes.Create();
        aes.Key  = _key;
        aes.Mode = CipherMode.CBC;

        // İlk 16 byte IV
        var iv   = raw[..16];
        var data = raw[16..];
        aes.IV   = iv;

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
