using Core.Database;
using Std.Database.Index;
using Std.Database.Storage;
using Std.Database.Transaction;
using Std.Database.Wal;

namespace Std.Database;

/// <summary>
///     LightDB 数据库构建器实现
/// </summary>
public sealed class LightDbBuilder : IDatabaseBuilder
{
    #region IDatabaseBuilder 实现

    /// <inheritdoc />
    public IDatabase Build()
    {
        var storage = _storage_engine ?? new FileStorageEngine(_storage_options.path, _storage_options.page_size);
        var pageCache = _page_cache ?? new PageCache(storage, _storage_options.page_cache_size);
        var walWriter = _wal_writer ?? new WalWriter(Path.Combine(_storage_options.path, "light.wal"));
        var transactionManager = new TransactionManager(walWriter);
        var versionStore = new VersionStore();
        var primaryIndex =
            _primary_index ?? new BTreeIndex(pageCache, "primary", _index_options.b_tree_order, versionStore);

        return new LightDb(
            _storage_options,
            _index_options,
            _wal_options,
            storage,
            pageCache,
            walWriter,
            transactionManager,
            versionStore,
            primaryIndex);
    }

    #endregion

    #region 字段

    private BTreeIndexOptions _index_options = BTreeIndexOptions.@default;
    private IPageCache? _page_cache;
    private IBTreeIndex? _primary_index;
    private IStorageEngine? _storage_engine;
    private StorageOptions _storage_options = StorageOptions.@default;
    private WalOptions _wal_options = WalOptions.@default;
    private IWalWriter? _wal_writer;

    #endregion

    #region 流式配置

    /// <summary>
    ///     配置存储引擎选项
    /// </summary>
    /// <param name="options">存储配置</param>
    /// <returns>构建器实例</returns>
    internal LightDbBuilder with_storage_options(StorageOptions options)
    {
        _storage_options = options;
        return this;
    }

    /// <summary>
    ///     配置索引引擎选项
    /// </summary>
    /// <param name="options">索引配置</param>
    /// <returns>构建器实例</returns>
    internal LightDbBuilder with_index_options(BTreeIndexOptions options)
    {
        _index_options = options;
        return this;
    }

    /// <summary>
    ///     配置 WAL 日志选项
    /// </summary>
    /// <param name="options">WAL 配置</param>
    /// <returns>构建器实例</returns>
    internal LightDbBuilder with_wal_options(WalOptions options)
    {
        _wal_options = options;
        return this;
    }

    /// <summary>
    ///     使用自定义存储引擎
    /// </summary>
    /// <param name="storageEngine">存储引擎</param>
    /// <returns>构建器实例</returns>
    internal LightDbBuilder use_storage_engine(IStorageEngine storageEngine)
    {
        _storage_engine = storageEngine;
        return this;
    }

    /// <summary>
    ///     使用自定义页缓存
    /// </summary>
    /// <param name="pageCache">页缓存</param>
    /// <returns>构建器实例</returns>
    internal LightDbBuilder use_page_cache(IPageCache pageCache)
    {
        _page_cache = pageCache;
        return this;
    }

    /// <summary>
    ///     使用自定义 WAL 写入器
    /// </summary>
    /// <param name="walWriter">WAL 写入器</param>
    /// <returns>构建器实例</returns>
    internal LightDbBuilder use_wal_writer(IWalWriter walWriter)
    {
        _wal_writer = walWriter;
        return this;
    }

    /// <summary>
    ///     使用自定义主索引
    /// </summary>
    /// <param name="primaryIndex">主索引</param>
    /// <returns>构建器实例</returns>
    internal LightDbBuilder use_primary_index(IBTreeIndex primaryIndex)
    {
        _primary_index = primaryIndex;
        return this;
    }

    /// <summary>
    ///     创建默认构建器
    /// </summary>
    /// <returns>构建器实例</returns>
    public static LightDbBuilder create()
    {
        return new LightDbBuilder();
    }

    #endregion
}