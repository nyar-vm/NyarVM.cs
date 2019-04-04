namespace Std.Compression;

/// <summary>
///     流式压缩写入器接口，将原始数据压缩后写入底层流。
///     遵循 Oak 的编码器模式（写出 = 编码）。
/// </summary>
public interface ICompressionWriter : IDisposable
{
    /// <summary>
    ///     将原始数据压缩后写入底层流。
    /// </summary>
    /// <param name="data">待压缩的原始数据</param>
    void Write(ReadOnlySpan<byte> data);
}