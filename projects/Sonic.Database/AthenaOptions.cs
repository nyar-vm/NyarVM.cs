namespace Olympus.Athena;

#region AthenaOptions 引擎配置

/// <summary>
///     Athena 嵌入式数据库引擎配置选项
/// </summary>
public sealed class AthenaOptions
{
    #region 构造函数

    /// <summary>
    ///     创建 Athena 引擎配置，所有参数均可选
    /// </summary>
    /// <param name="dataDirectory">数据存储目录路径，默认 "./athena_data"</param>
    /// <param name="memoryCacheBytes">内存缓存上限字节数，默认 256MB</param>
    /// <param name="microPartitionRows">每个微分区最大行数，默认 10000</param>
    /// <param name="defaultIndexType">默认向量索引类型，默认 "hnsw"</param>
    /// <param name="enableDiskCache">是否启用磁盘缓存，默认 true</param>
    public AthenaOptions(
        string? dataDirectory = null,
        long? memoryCacheBytes = null,
        int? microPartitionRows = null,
        string? defaultIndexType = null,
        bool? enableDiskCache = null)
    {
        DataDirectory = dataDirectory ?? "./athena_data";
        MemoryCacheBytes = memoryCacheBytes ?? 256L * 1024 * 1024;
        MicroPartitionRows = microPartitionRows ?? 10000;
        DefaultIndexType = defaultIndexType ?? "hnsw";
        EnableDiskCache = enableDiskCache ?? true;
    }

    #endregion

    #region 静态默认值

    /// <summary>
    ///     获取默认配置实例
    /// </summary>
    public static AthenaOptions Default { get; } = new();

    #endregion

    #region 属性

    /// <summary>
    ///     数据存储目录路径，默认为 "./athena_data"
    /// </summary>
    public string DataDirectory { get; init; }

    /// <summary>
    ///     内存缓存上限字节数，默认为 256MB（268435456 字节）
    /// </summary>
    public long MemoryCacheBytes { get; init; }

    /// <summary>
    ///     每个微分区容纳的最大行数，默认为 10000
    /// </summary>
    public int MicroPartitionRows { get; init; }

    /// <summary>
    ///     默认向量索引类型，默认为 "hnsw"
    /// </summary>
    public string DefaultIndexType { get; init; }

    /// <summary>
    ///     是否启用磁盘缓存，默认为 <c>true</c>
    /// </summary>
    public bool EnableDiskCache { get; init; }

    #endregion
}

#endregion