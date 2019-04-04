using System.Security.Cryptography;
using Core.Security.Cryptography;

namespace Std.Security.Cryptography;

/// <summary>
///     SHA-256 哈希算法实现，提供 <c>compute_hash</c> 和 <c>verify</c> 方法。
/// </summary>
public sealed class Sha256Hasher : IHashAlgorithm
{
    /// <summary>
    ///     计算数据的 SHA-256 哈希值。
    /// </summary>
    /// <param name="data">输入数据。</param>
    /// <returns>32 字节的 SHA-256 哈希值。</returns>
    public byte[] hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
    }

    /// <summary>
    ///     验证数据是否与给定的哈希值匹配。
    /// </summary>
    /// <param name="data">输入数据。</param>
    /// <param name="expectedHash">期望的哈希值。</param>
    /// <returns>如果哈希值匹配则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public bool verify(byte[] data, byte[] expectedHash)
    {
        var actualHash = hash(data);

        if (actualHash.Length != expectedHash.Length) return false;

        for (var i = 0; i < actualHash.Length; i++)
            if (actualHash[i] != expectedHash[i])
                return false;

        return true;
    }
}