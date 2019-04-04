using System.IO.Compression;
using Core.Data.Compression;

namespace Sonic.Compression;

/// <summary>
/// Brotli 压缩器，提供 Brotli 算法的数据压缩与解压缩
/// </summary>
public sealed class BrotliCompressor : ICompressor, IDecompressor
{
    /// <summary>
    /// 压缩级别
    /// </summary>
    private readonly CompressionLevel _level;

    /// <summary>
    /// 初始化 Brotli 压缩器
    /// </summary>
    /// <param name="level">压缩级别，默认为最优压缩</param>
    public BrotliCompressor(CompressionLevel level = CompressionLevel.Optimal)
    {
        _level = level;
    }

    /// <summary>
    /// 压缩输入字节数据
    /// </summary>
    /// <param name="input">待压缩的原始字节数据</param>
    /// <returns>压缩后的字节数据</returns>
    public byte[] compress(byte[] input)
    {
        using var output = new System.IO.MemoryStream();
        using (var brotli = new BrotliStream(output, _level))
        {
            brotli.Write(input, 0, input.Length);
        }
        return output.ToArray();
    }

    /// <summary>
    /// 解压缩输入字节数据
    /// </summary>
    /// <param name="input">待解压缩的字节数据</param>
    /// <returns>解压缩后的原始字节数据</returns>
    public byte[] decompress(byte[] input)
    {
        using var source = new System.IO.MemoryStream(input);
        using var brotli = new BrotliStream(source, CompressionMode.Decompress);
        using var output = new System.IO.MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
}