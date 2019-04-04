namespace Std.Database.Storage;

/// <summary>
///     页面压缩器接口
/// </summary>
internal interface IPageCompressor
{
    /// <summary>
    ///     最大压缩后大小
    /// </summary>
    /// <param name="uncompressedSize">未压缩大小</param>
    /// <returns>最大压缩后字节数</returns>
    int max_compressed_size(int uncompressedSize);

    /// <summary>
    ///     压缩数据
    /// </summary>
    /// <param name="source">源数据</param>
    /// <param name="destination">目标缓冲区</param>
    /// <returns>压缩后字节数，失败返回 -1</returns>
    int compress(ReadOnlySpan<byte> source, Span<byte> destination);

    /// <summary>
    ///     解压数据
    /// </summary>
    /// <param name="source">源数据</param>
    /// <param name="destination">目标缓冲区</param>
    /// <returns>解压后字节数，失败返回 -1</returns>
    int decompress(ReadOnlySpan<byte> source, Span<byte> destination);
}