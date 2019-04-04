namespace Core.Security.Cryptography;

/// <summary>
///     哈希算法类型
/// </summary>
public enum HashAlgorithm
{
    /// <summary>
    ///     SHA-256 哈希算法
    /// </summary>
    sha256,

    /// <summary>
    ///     SHA-512 哈希算法
    /// </summary>
    sha512,

    /// <summary>
    ///     BLAKE3 哈希算法
    /// </summary>
    blake3
}