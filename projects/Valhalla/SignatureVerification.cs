namespace Valhalla;

/// <summary>
///     Ed25519 签名及验证结果
/// </summary>
[Obsolete("Ed25519 自建实现不符合 RFC 8032 标准，请迁移到 NSec.Cryptography 库。")]
public class SignatureVerification
{
    public bool valid { get; set; }
    public string? public_key_fingerprint { get; set; }
    public string? error { get; set; }

    public static SignatureVerification success(string fingerprint)
    {
        return new SignatureVerification { valid = true, public_key_fingerprint = fingerprint };
    }

    public static SignatureVerification fail(string error)
    {
        return new SignatureVerification { valid = false, error = error };
    }
}