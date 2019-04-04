namespace Core.Security.Cryptography;

/// <summary>
///     哈希算法接口
/// </summary>
public interface IHashAlgorithm
{
    /// <summary>
    ///     计算数据的哈希值
    /// </summary>
    /// <param name="data">输入数据</param>
    /// <returns>哈希值</returns>
    byte[] hash(byte[] data);
}