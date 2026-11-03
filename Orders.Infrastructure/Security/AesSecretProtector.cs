using System.Security.Cryptography;
using System.Text;
using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Security;

public sealed class AesSecretProtector(string keyBase64) : ISecretProtector
{
    private readonly byte[] _key = Decode(keyBase64);

    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return string.Join('.',
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(cipher));
    }

    public string Unprotect(string protectedValue)
    {
        var parts = protectedValue.Split('.');
        if (parts.Length != 3)
            throw new CryptographicException("El secreto guardado no tiene el formato esperado.");

        var nonce = Convert.FromBase64String(parts[0]);
        var tag = Convert.FromBase64String(parts[1]);
        var cipher = Convert.FromBase64String(parts[2]);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] Decode(string keyBase64)
    {
        var key = Convert.FromBase64String(keyBase64);
        if (key.Length != 32)
            throw new InvalidOperationException("DataProtection:Key tiene que ser una clave de 32 bytes en base64.");
        return key;
    }
}
