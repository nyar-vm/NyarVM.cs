namespace Core.Data.Compression;

/// <summary>
///     压缩算法类型
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    ///     Gzip 压缩算法
    /// </summary>
    gzip,

    /// <summary>
    ///     Deflate 压缩算法
    /// </summary>
    deflate,

    /// <summary>
    ///     Brotli 压缩算法
    /// </summary>
    brotli,

    /// <summary>
    ///     Zstandard 压缩算法
    /// </summary>
    zstd,

    /// <summary>
    ///     LZ4 压缩算法
    /// </summary>
    lz4
}