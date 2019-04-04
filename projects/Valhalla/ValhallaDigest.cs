using System.Security.Cryptography;

namespace Valhalla;

/// <summary>
///     SHA-256 摘要，用于二进制内容的完整性承诺
/// </summary>
public readonly struct ValhallaDigest : IEquatable<ValhallaDigest>
{
    private readonly byte[] _hash;

    /// <summary>
    ///     以 hex 字符串表示的 SHA-256
    /// </summary>
    public string hex_string { get; }

    /// <summary>
    ///     从 hex 字符串创建
    /// </summary>
    public ValhallaDigest(string hexString)
    {
        if (string.IsNullOrWhiteSpace(hexString) || hexString.Length != 64)
            throw new ArgumentException("SHA-256 must be 64 hex characters", nameof(hexString));

        _hash = new byte[32];

        for (var i = 0; i < 32; i++) _hash[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);

        hex_string = hexString.ToLowerInvariant();
    }

    /// <summary>
    ///     从字节数组创建
    /// </summary>
    public ValhallaDigest(byte[] hash)
    {
        if (hash.Length != 32) throw new ArgumentException("SHA-256 must be 32 bytes", nameof(hash));

        _hash = new byte[32];
        Array.Copy(hash, _hash, 32);
        hex_string = Convert.ToHexString(_hash).ToLowerInvariant();
    }

    /// <summary>
    ///     从流计算 SHA-256
    /// </summary>
    public static async Task<ValhallaDigest> compute(Stream stream, CancellationToken ct = default)
    {
        var hash = await SHA256.HashDataAsync(stream, ct);
        return new ValhallaDigest(hash);
    }

    /// <summary>
    ///     从字节数组计算 SHA-256
    /// </summary>
    public static ValhallaDigest compute(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return new ValhallaDigest(hash);
    }

    public bool Equals(ValhallaDigest other)
    {
        return hex_string == other.hex_string;
    }

    public override bool Equals(object? obj)
    {
        return obj is ValhallaDigest other && Equals(other);
    }

    public override int GetHashCode()
    {
        return hex_string.GetHashCode();
    }

    public override string ToString()
    {
        return $"sha256:{hex_string}";
    }

    public static bool operator ==(ValhallaDigest left, ValhallaDigest right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ValhallaDigest left, ValhallaDigest right)
    {
        return !left.Equals(right);
    }
}