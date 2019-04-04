namespace Core.Security.Cryptography;

/// <summary>
///     对称加密接口
/// </summary>
public interface ISymmetricCipher
{
    /// <summary>
    ///     使用密钥加密明文
    /// </summary>
    /// <param name="key">加密密钥</param>
    /// <param name="plaintext">明文数据</param>
    /// <returns>密文数据</returns>
    byte[] encrypt(byte[] key, byte[] plaintext);

    /// <summary>
    ///     使用密钥解密密文
    /// </summary>
    /// <param name="key">解密密钥</param>
    /// <param name="ciphertext">密文数据</param>
    /// <returns>明文数据</returns>
    byte[] decrypt(byte[] key, byte[] ciphertext);
}