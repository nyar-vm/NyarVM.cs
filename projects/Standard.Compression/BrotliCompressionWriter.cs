using System;
using System.IO;
using System.IO.Compression;

namespace Std.Compression;

/// <summary>
///     Brotli 流式压缩写入器，将数据压缩后写入底层流。
/// </summary>
public sealed class BrotliCompressionWriter : ICompressionWriter
{
    private readonly BrotliStream _brotliStream;

    /// <summary>
    ///     初始化 Brotli 流式压缩写入器。
    /// </summary>
    /// <param name="outputStream">压缩数据输出目标流</param>
    /// <param name="level">压缩级别，默认为最优压缩</param>
    public BrotliCompressionWriter(System.IO.Stream outputStream, CompressionLevel level = CompressionLevel.Optimal)
    {
        _brotliStream = new BrotliStream(outputStream, level);
    }

    /// <summary>
    ///     将原始数据压缩后写入底层流。
    /// </summary>
    /// <param name="data">待压缩的原始数据</param>
    public void Write(ReadOnlySpan<byte> data)
    {
        _brotliStream.Write(data);
        _brotliStream.Flush();
    }

    /// <summary>
    ///     释放 Brotli 压缩流资源，确保所有压缩数据写入底层流。
    /// </summary>
    public void Dispose()
    {
        _brotliStream.Dispose();
    }
}