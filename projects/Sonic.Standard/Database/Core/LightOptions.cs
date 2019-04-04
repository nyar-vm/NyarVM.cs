using Core.Database;
using Std.Database.Index;
using Std.Database.Storage;
using Std.Database.Wal;

namespace Std.Database.Core;

/// <summary>
///     LightDB 配置选项
/// </summary>
public sealed class LightOptions : DatabaseOptions
{
    /// <summary>
    ///     数据库路径
    /// </summary>
    public string path { get; set; } = ".light/db";

    /// <summary>
    ///     存储引擎类型
    /// </summary>
    [Obsolete("LSM 引擎已移除，仅支持 BTree")]
    public StorageEngineType engine_type { get; set; } = StorageEngineType.b_tree;

    /// <summary>
    ///     B+ 树阶数
    /// </summary>
    public int b_tree_order { get; set; } = 128;

    /// <summary>
    ///     是否只读
    /// </summary>
    public bool read_only { get; set; } = false;

    /// <summary>
    ///     是否自动检查点
    /// </summary>
    public bool auto_checkpoint { get; set; } = true;

    /// <summary>
    ///     检查点间隔（毫秒）
    /// </summary>
    public int checkpoint_interval_ms { get; set; } = 30000;

    /// <summary>
    ///     WAL 刷盘策略
    /// </summary>
    public WalFlushPolicy wal_flush_policy { get; set; } = WalFlushPolicy.batch;

    /// <summary>
    ///     是否启用 WAL 压缩（Brotli）
    /// </summary>
    public bool enable_wal_compression { get; set; } = false;

    /// <summary>
    ///     WAL 压缩阈值（字节），小于此大小的记录不压缩
    /// </summary>
    public int wal_compression_threshold { get; set; } = 256;

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
    public static LightOptions @default => new();

    /// <summary>
    ///     转换为存储引擎配置
    /// </summary>
    internal StorageOptions to_storage_options()
    {
        return new StorageOptions
        {
            path = path,
            page_size = PageSize,
            read_only = read_only,
            page_cache_size = CacheSize,
            enable_compression = enable_compression,
            enable_memory_mapping = enable_memory_mapping
        };
    }

    /// <summary>
    ///     转换为索引引擎配置
    /// </summary>
    internal BTreeIndexOptions to_index_options()
    {
        return new BTreeIndexOptions
        {
            b_tree_order = b_tree_order
        };
    }

    /// <summary>
    ///     转换为 WAL 日志配置
    /// </summary>
    internal WalOptions to_wal_options()
    {
        return new WalOptions
        {
            auto_checkpoint = auto_checkpoint,
            checkpoint_interval_ms = checkpoint_interval_ms,
            wal_flush_policy = wal_flush_policy,
            enable_wal_compression = enable_wal_compression,
            wal_compression_threshold = wal_compression_threshold
        };
    }
}