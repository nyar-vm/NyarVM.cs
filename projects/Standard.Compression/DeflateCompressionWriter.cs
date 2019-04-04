using System;
using System.IO;
using System.IO.Compression;

namespace Std.Compression;

/// <summary>
///     Deflate 流式压缩写入器，将数据压缩后写入底层流。
/// </summary>
public sealed class DeflateCompressionWriter : ICompressionWriter
{
    private readonly DeflateStream _deflateStream;

    /// <summary>
    ///     初始化 Deflate 流式压缩写入器。
    /// </summary>
    /// <param name="outputStream">压缩数据输出目标流</param>
    /// <param name="level">压缩级别，默认为最优压缩</param>
    public DeflateCompressionWriter(System.IO.Stream outputStream, CompressionLevel level = CompressionLevel.Optimal)
    {
        _deflateStream = new DeflateStream(outputStream, level);
    }

    /// <summary>
    ///     将原始数据压缩后写入底层流。
    /// </summary>
    /// <param name="data">待压缩的原始数据</param>
    public void Write(ReadOnlySpan<byte> data)
    {
        _deflateStream.Write(data);
        _deflateStream.Flush();
    }

    /// <summary>
    ///     释放 Deflate 压缩流资源，确保所有压缩数据写入底层流。
    /// </summary>
    public void Dispose()
    {
        _deflateStream.Dispose();
    }
}