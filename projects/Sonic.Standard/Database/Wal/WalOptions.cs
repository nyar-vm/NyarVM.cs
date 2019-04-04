namespace Std.Database.Wal;

/// <summary>
///     WAL 配置选项
/// </summary>
internal sealed class WalOptions
{
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
    ///     默认配置
    /// </summary>
    public static WalOptions @default => new();
}