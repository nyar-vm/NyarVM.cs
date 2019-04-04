using Core.Database;
using Std.Database.Core;
using Std.Database.Index;
using Std.Database.Storage;
using Std.Database.Transaction;
using Std.Database.Wal;

namespace Std.Database;

/// <summary>
///     LightDB 数据库实现
/// </summary>
public sealed class LightDb : IDatabase
{
    #region 集合

    /// <summary>
    ///     获取文档集合（内部 API）
    /// </summary>
    /// <typeparam name="TDocument">文档类型</typeparam>
    /// <param name="collectionName">集合名称</param>
    /// <returns>集合实例</returns>
    internal global::Core.Database.ICollection<TDocument> get_collection<TDocument>(string collectionName)
        where TDocument : class
    {
        return new LightCollection<TDocument>(collectionName, this);
    }

    #endregion

    #region 游标

    /// <summary>
    ///     创建游标
    /// </summary>
    public ICursor CreateCursor()
    {
        return new LightCursor(_write_coordinator.primary_index, DatabaseKey.empty);
    }

    /// <summary>
    ///     从指定键创建游标（内部 API）
    /// </summary>
    /// <param name="key">起始键</param>
    /// <returns>游标实例</returns>
    internal LightCursor seek(DatabaseKey key)
    {
        return new LightCursor(_write_coordinator.primary_index, key);
    }

    #endregion

    #region 字段

    private readonly IStorageEngine _storage;
    private readonly IPageCache _page_cache;
    private readonly IWalWriter _wal_writer;
    private readonly TransactionManager _transaction_manager;
    private readonly WriteCoordinator _write_coordinator;
    private readonly VersionStore _version_store;
    private readonly Dictionary<string, IBTreeIndex> _secondary_indexes;
    private readonly Timer? _checkpoint_timer;
    private readonly string _storage_path;
    private bool _disposed;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建 LightDB 数据库（使用默认配置）
    /// </summary>
    /// <param name="options">配置选项</param>
    public LightDb(LightOptions? options = null)
    {
        var opts = options ?? LightOptions.@default;
        var storageOptions = opts.to_storage_options();
        var indexOptions = opts.to_index_options();
        var walOptions = opts.to_wal_options();

        _storage_path = storageOptions.path;

        IStorageEngine baseStorage;
        if (storageOptions.enable_memory_mapping)
            baseStorage =
                new MemoryMappedStorageEngine(storageOptions.path, storageOptions.page_size, storageOptions.read_only);
        else
            baseStorage = new FileStorageEngine(storageOptions.path, storageOptions.page_size);

        if (storageOptions.enable_compression)
        {
            var compressMetaPath = Path.Combine(storageOptions.path, "light.compress.meta");
            _storage = new CompressingStorageEngine(baseStorage, new BrotliPageCompressor(), compressMetaPath);
        }
        else
        {
            _storage = baseStorage;
        }

        _page_cache = new PageCache(_storage, storageOptions.page_cache_size);
        _wal_writer = new WalWriter(Path.Combine(storageOptions.path, "light.wal"),
            walOptions.wal_flush_policy, 64, 100,
            walOptions.enable_wal_compression, walOptions.wal_compression_threshold);
        _transaction_manager = new TransactionManager(_wal_writer);

        _version_store = new VersionStore();
        var primaryIndex = new BTreeIndex(_page_cache, "primary", indexOptions.b_tree_order, _version_store);
        _write_coordinator = new WriteCoordinator(primaryIndex, _wal_writer, _transaction_manager);

        _secondary_indexes = new Dictionary<string, IBTreeIndex>();
        statistics = new DatabaseStatistics();

        if (!try_recover_from_metadata(primaryIndex)) recover_from_wal(storageOptions.path);

        if (walOptions.auto_checkpoint)
            _checkpoint_timer = new Timer(
                async _ => await check_point(),
                null,
                walOptions.checkpoint_interval_ms,
                walOptions.checkpoint_interval_ms);
    }

    /// <summary>
    ///     从 WAL 恢复数据
    /// </summary>
    /// <param name="storagePath">存储路径</param>
    private void recover_from_wal(string storagePath)
    {
        var walPath = Path.Combine(storagePath, "light.wal");
        if (!File.Exists(walPath)) return;

        using var reader = new WalReader(walPath);
        var records = reader.read_all().ToListAsync().GetAwaiter().GetResult();

        var committedTxIds = new HashSet<TransactionId>();
        foreach (var record in records)
            if (record.operation_type == WalOperationType.commit_transaction)
                committedTxIds.Add(record.transaction_id);

        foreach (var record in records)
        {
            var isCommitted = record.transaction_id == TransactionId.min
                              || committedTxIds.Contains(record.transaction_id);

            if (!isCommitted) continue;

            switch (record.operation_type)
            {
                case WalOperationType.update:
                    if (record.new_value is not null)
                        _write_coordinator.primary_index.insert(record.key, record.new_value.Value, record.sequence)
                            .GetAwaiter().GetResult();

                    break;
                case WalOperationType.delete:
                    _write_coordinator.primary_index.delete(record.key, record.sequence).GetAwaiter().GetResult();
                    break;
            }
        }

        _page_cache.flush(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     从元数据文件恢复数据库状态（优先于 WAL 恢复）
    /// </summary>
    /// <param name="primaryIndex">主索引</param>
    /// <returns>是否成功从元数据恢复</returns>
    private bool try_recover_from_metadata(IBTreeIndex primaryIndex)
    {
        var metaPath = Path.Combine(_storage_path, "light.meta");
        if (!File.Exists(metaPath)) return false;

        try
        {
            var lines = File.ReadAllLines(metaPath);
            var metadata = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                var separatorIndex = line.IndexOf('=');
                if (separatorIndex > 0)
                {
                    var key = line[..separatorIndex].Trim();
                    var value = line[(separatorIndex + 1)..].Trim();
                    metadata[key] = value;
                }
            }

            if (metadata.TryGetValue("RootPageId", out var rootPageIdStr) &&
                long.TryParse(rootPageIdStr, out var rootPageId))
                if (primaryIndex is BTreeIndex btree && rootPageId >= 0)
                {
                    btree.restore_root_page_id(rootPageId);

                    recover_wal_incremental();

                    return true;
                }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     元数据恢复后增量重放 Checkpoint 之后的 WAL 记录
    /// </summary>
    private void recover_wal_incremental()
    {
        var walPath = Path.Combine(_storage_path, "light.wal");
        if (!File.Exists(walPath)) return;

        using var reader = new WalReader(walPath);
        var records = reader.read_all().ToListAsync().GetAwaiter().GetResult();

        SequenceNumber? checkpointSequence = null;
        for (var i = records.Count - 1; i >= 0; i--)
            if (records[i].operation_type == WalOperationType.checkpoint)
            {
                checkpointSequence = records[i].sequence;
                break;
            }

        var startSequence = checkpointSequence?.value + 1 ?? 0UL;

        var committedTxIds = new HashSet<TransactionId>();
        foreach (var record in records)
        {
            if (record.sequence.value < startSequence) continue;

            if (record.operation_type == WalOperationType.commit_transaction) committedTxIds.Add(record.transaction_id);
        }

        foreach (var record in records)
        {
            if (record.sequence.value < startSequence) continue;

            var isCommitted = record.transaction_id == TransactionId.min
                              || committedTxIds.Contains(record.transaction_id);

            if (!isCommitted) continue;

            switch (record.operation_type)
            {
                case WalOperationType.update:
                    if (record.new_value is not null)
                        _write_coordinator.primary_index.insert(record.key, record.new_value.Value, record.sequence)
                            .GetAwaiter().GetResult();

                    break;
                case WalOperationType.delete:
                    _write_coordinator.primary_index.delete(record.key, record.sequence).GetAwaiter().GetResult();
                    break;
            }
        }

        _page_cache.flush(CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    ///     保存元数据文件（checkpoint 时调用）
    /// </summary>
    private void save_metadata()
    {
        var metaPath = Path.Combine(_storage_path, "light.meta");
        var primaryIndex = _write_coordinator.primary_index;

        if (primaryIndex is BTreeIndex btree)
        {
            var content = $"RootPageId={btree.root_page_id}";
            var tempPath = metaPath + ".tmp";
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, metaPath, true);
        }
    }

    /// <summary>
    ///     创建 LightDB 数据库（依赖注入构造函数）
    /// </summary>
    internal LightDb(
        StorageOptions storageOptions,
        BTreeIndexOptions indexOptions,
        WalOptions walOptions,
        IStorageEngine storage,
        IPageCache pageCache,
        IWalWriter walWriter,
        TransactionManager transactionManager,
        VersionStore versionStore,
        IBTreeIndex primaryIndex)
    {
        _storage_path = storageOptions.path;
        _storage = storage;
        _page_cache = pageCache;
        _wal_writer = walWriter;
        _transaction_manager = transactionManager;
        _version_store = versionStore;
        _write_coordinator = new WriteCoordinator(primaryIndex, walWriter, transactionManager);
        _secondary_indexes = new Dictionary<string, IBTreeIndex>();
        statistics = new DatabaseStatistics();

        if (!try_recover_from_metadata(primaryIndex)) recover_from_wal(storageOptions.path);

        if (walOptions.auto_checkpoint)
            _checkpoint_timer = new Timer(
                async _ => await check_point(),
                null,
                walOptions.checkpoint_interval_ms,
                walOptions.checkpoint_interval_ms);
    }

    #endregion

    #region 属性

    /// <inheritdoc />
    public string name => "LightDB";

    /// <summary>
    ///     配置选项
    /// </summary>
    public LightOptions options { get; } = LightOptions.@default;

    /// <summary>
    ///     统计信息
    /// </summary>
    public DatabaseStatistics statistics { get; }

    /// <inheritdoc />
    public IIndexManager IndexManager => throw new NotImplementedException();

    #endregion

    #region IDatabase 实现

    /// <inheritdoc />
    public Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        return get<byte[]>(dbKey, cancellationToken).AsTask().ContinueWith(t =>
        {
            if (t.Result is null) return (ReadOnlyMemory<byte>?)null;

            return new ReadOnlyMemory<byte>(t.Result);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        await put(dbKey, value.ToArray(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        await delete(dbKey, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ContainsKeyAsync(ReadOnlyMemory<byte> key, CancellationToken cancellationToken = default)
    {
        var dbKey = new DatabaseKey(key.ToArray());
        return await contains(dbKey, cancellationToken);
    }

    /// <inheritdoc />
    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot)
    {
        var tx = _transaction_manager.begin_transaction(isolationLevel, _write_coordinator, CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();
        statistics.active_transactions = _transaction_manager.active_transaction_count;
        return new LightTransactionAdapter(tx);
    }

    /// <inheritdoc />
    public ISnapshot CreateSnapshot()
    {
        var sequence = _transaction_manager.current_sequence;
        return new LightSnapshotAdapter(new LightSnapshot(sequence, _write_coordinator.primary_index));
    }

    #endregion

    #region 事务与快照（内部 API）

    /// <summary>
    ///     开启事务（内部 API）
    /// </summary>
    internal async ValueTask<LightTransaction> begin_transaction(
        IsolationLevel isolationLevel = IsolationLevel.Snapshot,
        CancellationToken cancellationToken = default)
    {
        var tx = await _transaction_manager.begin_transaction(isolationLevel, _write_coordinator, cancellationToken);
        statistics.active_transactions = _transaction_manager.active_transaction_count;
        return tx;
    }

    /// <summary>
    ///     创建快照（内部 API）
    /// </summary>
    internal LightSnapshot create_snapshot()
    {
        var sequence = _transaction_manager.current_sequence;
        return new LightSnapshot(sequence, _write_coordinator.primary_index);
    }

    #endregion

    #region CRUD 操作

    /// <summary>
    ///     获取键值（内部 API）
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>值</returns>
    internal async ValueTask<TValue?> get<TValue>(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        statistics.increment_read_count();

        var value = await _write_coordinator.get(key, cancellationToken: cancellationToken);
        if (value is null) return default;

        return value.Value.to_object<TValue>();
    }

    /// <summary>
    ///     存储键值（内部 API）
    /// </summary>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="cancellationToken">取消令牌</param>
    internal async ValueTask put<TValue>(DatabaseKey key, TValue value, CancellationToken cancellationToken = default)
    {
        statistics.increment_write_count();

        var lightValueBtree = DatabaseValue.from_object(value);
        await _write_coordinator.put(key, lightValueBtree, cancellationToken);
    }

    /// <summary>
    ///     删除键（内部 API）
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功删除</returns>
    internal async ValueTask<bool> delete(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        statistics.increment_delete_count();

        return await _write_coordinator.delete(key, cancellationToken);
    }

    /// <summary>
    ///     判断键是否存在（内部 API）
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    internal async ValueTask<bool> contains(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        var value = await _write_coordinator.get(key, cancellationToken: cancellationToken);
        return value is not null;
    }

    #endregion

    #region 二级索引

    /// <summary>
    ///     创建二级索引（内部 API）
    /// </summary>
    /// <param name="name">索引名称</param>
    internal async ValueTask create_secondary_index(string name)
    {
        if (!_secondary_indexes.ContainsKey(name)) _secondary_indexes[name] = new BTreeIndex(_page_cache, name);

        await Task.CompletedTask;
    }

    /// <summary>
    ///     创建二级索引并从已有数据中填充（内部 API）
    /// </summary>
    /// <param name="name">索引名称</param>
    /// <param name="prefix">数据前缀，用于扫描已有数据</param>
    /// <param name="fieldSelector">字段选择器，用于提取索引键</param>
    /// <typeparam name="TDocument">文档类型</typeparam>
    /// <typeparam name="TField">索引字段类型</typeparam>
    internal async ValueTask create_secondary_index<TDocument, TField>(string name, DatabaseKey prefix,
        Func<TDocument, TField> fieldSelector) where TDocument : class
    {
        if (!_secondary_indexes.ContainsKey(name))
        {
            var index = new BTreeIndex(_page_cache, name);
            _secondary_indexes[name] = index;

            await foreach (var entry in _write_coordinator.primary_index.prefix_scan(prefix))
            {
                var document = entry.value.to_object<TDocument>();
                if (document is not null)
                {
                    var fieldValue = fieldSelector(document);
                    if (fieldValue is not null)
                    {
                        var indexKey = DatabaseKeyPatterns.index(name);
                        var compositeKey = (DatabaseKey)$"{indexKey}:{fieldValue}:{entry.key}";
                        await index.insert(compositeKey, entry.value);
                    }
                }
            }
        }
    }

    #endregion

    #region 检查点

    /// <summary>
    ///     执行检查点：刷盘 → GC → 保存元数据 → 截断 WAL（内部 API）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    internal async ValueTask check_point(CancellationToken cancellationToken = default)
    {
        await _page_cache.flush(cancellationToken);

        var minActiveSequence = _transaction_manager.get_min_active_sequence();
        _version_store.cleanup(minActiveSequence);

        if (_write_coordinator.primary_index is BTreeIndex btree)
            await btree.compact(minActiveSequence, cancellationToken);

        save_metadata();

        var sequence = _transaction_manager.allocate_sequence();
        await _wal_writer.append(new WalRecord
        {
            sequence = sequence,
            transaction_id = TransactionId.min,
            operation_type = WalOperationType.checkpoint,
            key = DatabaseKey.empty
        }, cancellationToken);

        await _wal_writer.flush(cancellationToken);
        await _wal_writer.truncate(sequence);
    }

    /// <summary>
    ///     手动触发垃圾回收：清理过期版本 + 物理删除墓碑 + 压缩稀疏页面（内部 API）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>移除的条目数</returns>
    internal async ValueTask<int> garbage_collect(CancellationToken cancellationToken = default)
    {
        var minActiveSequence = _transaction_manager.get_min_active_sequence();
        _version_store.cleanup(minActiveSequence);

        if (_write_coordinator.primary_index is BTreeIndex btree)
        {
            var removed = await btree.compact(minActiveSequence, cancellationToken);
            await _page_cache.flush(cancellationToken);
            save_metadata();
            return removed;
        }

        return 0;
    }

    #endregion

    #region 资源释放

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        _checkpoint_timer?.Dispose();

        await check_point();
        await _wal_writer.DisposeAsync();
        await _page_cache.flush();
        await _storage.DisposeAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    #endregion

    #region 适配器

    private sealed class LightTransactionAdapter : ITransaction
    {
        private readonly LightTransaction _tx;

        public LightTransactionAdapter(LightTransaction tx)
        {
            _tx = tx;
        }

        public IsolationLevel IsolationLevel => _tx.isolation_level;

        public Task<ReadOnlyMemory<byte>?> GetAsync(ReadOnlyMemory<byte> key)
        {
            var dbKey = new DatabaseKey(key.ToArray());
            return _tx.get<byte[]>(dbKey).AsTask().ContinueWith(t =>
            {
                if (t.Result is null) return (ReadOnlyMemory<byte>?)null;

                return new ReadOnlyMemory<byte>(t.Result);
            });
        }

        public async Task PutAsync(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
        {
            var dbKey = new DatabaseKey(key.ToArray());
            await _tx.put(dbKey, value.ToArray());
        }

        public async Task DeleteAsync(ReadOnlyMemory<byte> key)
        {
            var dbKey = new DatabaseKey(key.ToArray());
            await _tx.delete(dbKey);
        }

        public async Task CommitAsync()
        {
            await _tx.commit();
        }

        public async Task RollbackAsync()
        {
            await _tx.rollback();
        }

        public void Dispose()
        {
            _tx.Dispose();
        }
    }

    private sealed class LightSnapshotAdapter : ISnapshot
    {
        private readonly LightSnapshot _snapshot;

        public LightSnapshotAdapter(LightSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public ReadOnlyMemory<byte>? Get(ReadOnlyMemory<byte> key)
        {
            var dbKey = new DatabaseKey(key.ToArray());
            var result = _snapshot.get<byte[]>(dbKey).AsTask().GetAwaiter().GetResult();
            if (result is null) return null;

            return new ReadOnlyMemory<byte>(result);
        }

        public ICursor CreateCursor()
        {
            return _snapshot.create_cursor_adapter();
        }

        public void Dispose()
        {
            _snapshot.Dispose();
        }
    }

    #endregion
}