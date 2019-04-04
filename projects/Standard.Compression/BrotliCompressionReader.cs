using System;
using System.IO;
using System.IO.Compression;

namespace Std.Compression;

/// <summary>
///     Brotli 流式解压读取器，从 Brotli 压缩流中读取并解压数据。
/// </summary>
public sealed class BrotliCompressionReader : ICompressionReader
{
    private readonly BrotliStream _brotliStream;

    /// <summary>
    ///     初始化 Brotli 流式解压读取器。
    /// </summary>
    /// <param name="compressedStream">Brotli 压缩数据的源流</param>
    public BrotliCompressionReader(System.IO.Stream compressedStream)
    {
        _brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
    }

    /// <summary>
    ///     从压缩流中读取并解压数据到缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>实际解压写入的字节数，0 表示已到达流末尾</returns>
    public int Read(Span<byte> buffer)
    {
        return _brotliStream.Read(buffer);
    }

    /// <summary>
    ///     释放 Brotli 解压流资源。
    /// </summary>
    public void Dispose()
    {
        _brotliStream.Dispose();
    }
}