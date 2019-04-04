namespace Std.Database.Storage;

/// <summary>
///     存储配置选项
/// </summary>
internal sealed class StorageOptions
{
    /// <summary>
    ///     数据库路径
    /// </summary>
    public string path { get; set; } = ".light/db";

    /// <summary>
    ///     页面大小（字节）
    /// </summary>
    public int page_size { get; set; } = 4096;

    /// <summary>
    ///     是否只读
    /// </summary>
    public bool read_only { get; set; } = false;

    /// <summary>
    ///     页缓存大小（页面数）
    /// </summary>
    public int page_cache_size { get; set; } = 1024;

    /// <summary>
    ///     压缩启用
    /// </summary>
    public bool enable_compression { get; set; } = false;

    /// <summary>
    ///     内存映射模式启用
    /// </summary>
    public bool enable_memory_mapping { get; set; } = false;

    /// <summary>
    ///     默认配置
    /// </summary>
    public static StorageOptions @default => new();
}