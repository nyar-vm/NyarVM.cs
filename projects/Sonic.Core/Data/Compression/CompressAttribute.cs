using System;

namespace Core.Data.Compression;

/// <summary>
///     标记类或方法启用压缩，支持指定压缩算法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CompressAttribute : Attribute
{
    /// <summary>
    ///     压缩算法
    /// </summary>
    public CompressionAlgorithm algorithm { get; set; } = CompressionAlgorithm.gzip;
}