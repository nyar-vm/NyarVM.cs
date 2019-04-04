namespace Core.Security.Cryptography;

/// <summary>
///     非对称签名接口
/// </summary>
public interface IAsymmetricSigner
{
    /// <summary>
    ///     使用私钥对数据签名
    /// </summary>
    /// <param name="privateKey">私钥</param>
    /// <param name="data">待签名数据</param>
    /// <returns>签名结果</returns>
    byte[] sign(byte[] privateKey, byte[] data);

    /// <summary>
    ///     使用公钥验证签名
    /// </summary>
    /// <param name="publicKey">公钥</param>
    /// <param name="data">原始数据</param>
    /// <param name="signature">待验证的签名</param>
    /// <returns>签名是否有效</returns>
    bool verify(byte[] publicKey, byte[] data, byte[] signature);
}