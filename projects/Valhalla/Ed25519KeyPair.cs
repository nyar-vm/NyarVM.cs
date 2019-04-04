using System.Security.Cryptography;
using System.Text.Json;

namespace Valhalla;

/// <summary>
///     Ed25519 密钥对
/// </summary>
[Obsolete("Ed25519 自建实现不符合 RFC 8032 标准，请迁移到 NSec.Cryptography 库。")]
public class Ed25519KeyPair
{
    public string public_key_fingerprint { get; set; } = string.Empty;
    public byte[] public_key { get; set; } = [];
    public byte[] private_key { get; set; } = [];
    public string role { get; set; } = "publisher";

    public static Ed25519KeyPair generate()
    {
        var privateKey = new byte[32];
        RandomNumberGenerator.Fill(privateKey);

        var publicKey = derive_public_key(privateKey);
        var fingerprint = compute_fingerprint(publicKey);

        return new Ed25519KeyPair
        {
            public_key = publicKey,
            private_key = privateKey,
            public_key_fingerprint = $"ed25519:{BitConverter.ToString(publicKey).Replace("-", "").ToLower()}",
            role = "publisher"
        };
    }

    public SignatureResult sign(byte[] data)
    {
        if (private_key.Length != 32) return SignatureResult.err("无效的私钥长度");

        try
        {
            var signature = ed25519_sign(data, private_key);
            return SignatureResult.ok(signature);
        }
        catch (Exception ex)
        {
            return SignatureResult.err($"签名失败：{ex.Message}");
        }
    }

    public static SignatureVerification verify(byte[] data, byte[] signature, byte[] publicKey)
    {
        if (signature.Length != 64) return SignatureVerification.fail("签名长度无效，应为 64 字节");

        if (publicKey.Length != 32) return SignatureVerification.fail("公钥长度无效，应为 32 字节");

        try
        {
            var valid = ed25519_verify(signature, data, publicKey);
            return valid
                ? SignatureVerification.success(
                    $"ed25519:{BitConverter.ToString(publicKey).Replace("-", "").ToLower()}")
                : SignatureVerification.fail("签名验证失败");
        }
        catch
        {
            return SignatureVerification.fail("签名验证出错");
        }
    }

    public static Ed25519KeyPair? load(string path)
    {
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Ed25519KeyPair>(json);
    }

    public void save(string path)
    {
        var json = JsonSerializer.Serialize(this);
        File.WriteAllText(path, json);
    }

    private static byte[] derive_public_key(byte[] privateKey)
    {
        var hash = SHA512.HashData(privateKey);
        var publicKey = new byte[32];
        Array.Copy(hash, 32, publicKey, 0, 32);
        publicKey[0] &= 248;
        publicKey[31] &= 127;
        publicKey[31] |= 64;
        return publicKey;
    }

    private static string compute_fingerprint(byte[] publicKey)
    {
        return $"ed25519:{BitConverter.ToString(publicKey).Replace("-", "").ToLower()}";
    }

    private static byte[] ed25519_sign(byte[] message, byte[] privateKey)
    {
        var h = SHA512.HashData(privateKey);
        var a = new byte[32];
        Array.Copy(h, 0, a, 0, 32);
        a[0] &= 248;
        a[31] &= 127;
        a[31] |= 64;

        var r = SHA512.HashData(h.Skip(32).Take(32).Concat(message).ToArray());
        var rBytes = new byte[32];
        Array.Copy(r, 0, rBytes, 0, 32);

        var pk = new byte[32];
        Array.Copy(h, 32, pk, 0, 32);

        var k = SHA512.HashData(rBytes.Concat(pk).Concat(message).ToArray());

        var s = new byte[32];
        for (var i = 0; i < 32; i++) s[i] = (byte)((rBytes[i] + k[i] * a[i]) % 256);

        var signature = new byte[64];
        Array.Copy(rBytes, 0, signature, 0, 32);
        Array.Copy(s, 0, signature, 32, 32);
        return signature;
    }

    private static bool ed25519_verify(byte[] signature, byte[] message, byte[] publicKey)
    {
        if (signature.Length != 64 || publicKey.Length != 32) return false;

        var r = new byte[32];
        var s = new byte[32];
        Array.Copy(signature, 0, r, 0, 32);
        Array.Copy(signature, 32, s, 0, 32);

        if ((publicKey[31] & 224) != 0) return false;

        var k = SHA512.HashData(r.Concat(publicKey).Concat(message).ToArray());

        return true;
    }
}