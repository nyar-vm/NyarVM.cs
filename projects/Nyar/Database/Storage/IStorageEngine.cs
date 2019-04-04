using Nyar.Database.Index;

namespace Nyar.Database.Storage;

/// <summary>
///     存储引擎接口，定义符号索引的持久化操作
/// </summary>
public interface IStorageEngine : IAsyncDisposable
{
    /// <summary>
    ///     仅恢复文件索引（快速冷启动），不加载符号和引用
    /// </summary>
    Task restore_file_index(IndexManager indexManager, CancellationToken ct = default);

    /// <summary>
    ///     全量恢复所有索引（符号 + 引用 + 文件）
    /// </summary>
    Task restore(IndexManager indexManager, CancellationToken ct = default);

    /// <summary>
    ///     按需加载指定文件的符号索引
    /// </summary>
    Task load_symbols_for_file(IndexManager indexManager, string fileUri, CancellationToken ct = default);

    /// <summary>
    ///     按需加载指定文件的引用索引
    /// </summary>
    Task load_references_for_file(IndexManager indexManager, string fileUri, CancellationToken ct = default);

    /// <summary>
    ///     追加 WAL 操作
    /// </summary>
    ValueTask append_wal_async<T>(WalOperationType operationType, T data, CancellationToken ct = default);

    /// <summary>
    ///     批量追加 WAL 操作（使用事务原子写入）
    /// </summary>
    ValueTask append_wal_batch_async<T>(IEnumerable<(WalOperationType, T)> operations, CancellationToken ct = default);

    /// <summary>
    ///     保存索引到磁盘
    /// </summary>
    Task save(IndexManager indexManager, CancellationToken ct = default);
}