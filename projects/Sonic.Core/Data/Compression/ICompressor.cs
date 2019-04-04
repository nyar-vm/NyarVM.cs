namespace Core.Data.Compression;

/// <summary>
///     压缩器接口，提供数据压缩能力
/// </summary>
public interface ICompressor
{
    /// <summary>
    ///     压缩输入字节数据
    /// </summary>
    /// <param name="input">待压缩的原始字节数据</param>
    /// <returns>压缩后的字节数据</returns>
    byte[] compress(byte[] input);
}