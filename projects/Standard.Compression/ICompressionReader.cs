namespace Std.Compression;

/// <summary>
///     流式压缩读取器接口，从底层流读取压缩数据并解压。
///     遵循 Oak 的解码器模式（读入 = 解码）。
/// </summary>
public interface ICompressionReader : IDisposable
{
    /// <summary>
    ///     从底层流读取压缩数据，解压后写入目标缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区，解压后的数据将写入此处</param>
    /// <returns>实际解压写入的字节数，0 表示已到达流末尾</returns>
    int Read(Span<byte> buffer);
}