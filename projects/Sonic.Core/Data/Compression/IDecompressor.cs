namespace Core.Data.Compression;

/// <summary>
///     解压缩器接口，提供数据解压缩能力
/// </summary>
public interface IDecompressor
{
    /// <summary>
    ///     解压缩输入字节数据
    /// </summary>
    /// <param name="input">待解压缩的字节数据</param>
    /// <returns>解压缩后的原始字节数据</returns>
    byte[] decompress(byte[] input);
}