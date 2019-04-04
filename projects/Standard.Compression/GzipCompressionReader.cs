using System.IO;
using System.IO.Compression;

namespace Std.Compression;

/// <summary>
///     Gzip 流式解压读取器，从 Gzip 压缩流中读取并解压数据。
/// </summary>
public sealed class GzipCompressionReader : ICompressionReader
{
    private readonly GZipStream _gzipStream;

    /// <summary>
    ///     初始化 Gzip 流式解压读取器。
    /// </summary>
    /// <param name="compressedStream">Gzip 压缩数据的源流</param>
    public GzipCompressionReader(System.IO.Stream compressedStream)
    {
        _gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
    }

    /// <summary>
    ///     从压缩流中读取并解压数据到缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>实际解压写入的字节数，0 表示已到达流末尾</returns>
    public int Read(Span<byte> buffer)
    {
        return _gzipStream.Read(buffer);
    }

    /// <summary>
    ///     释放 Gzip 解压流资源。
    /// </summary>
    public void Dispose()
    {
        _gzipStream.Dispose();
    }
}