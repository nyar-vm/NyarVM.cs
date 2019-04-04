using System;
using System.IO;
using System.IO.Compression;

namespace Std.Compression;

/// <summary>
///     Deflate 流式解压读取器，从 Deflate 压缩流中读取并解压数据。
/// </summary>
public sealed class DeflateCompressionReader : ICompressionReader
{
    private readonly DeflateStream _deflateStream;

    /// <summary>
    ///     初始化 Deflate 流式解压读取器。
    /// </summary>
    /// <param name="compressedStream">Deflate 压缩数据的源流</param>
    public DeflateCompressionReader(System.IO.Stream compressedStream)
    {
        _deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
    }

    /// <summary>
    ///     从压缩流中读取并解压数据到缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>实际解压写入的字节数，0 表示已到达流末尾</returns>
    public int Read(Span<byte> buffer)
    {
        return _deflateStream.Read(buffer);
    }

    /// <summary>
    ///     释放 Deflate 解压流资源。
    /// </summary>
    public void Dispose()
    {
        _deflateStream.Dispose();
    }
}