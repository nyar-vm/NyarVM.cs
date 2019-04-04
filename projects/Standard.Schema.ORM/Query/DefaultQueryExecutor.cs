using Hermes.YYDB.Query;

namespace Hermes.ORM;

/// <summary>
///     默认查询执行器——将查询分发到各后端
/// </summary>
public sealed class DefaultQueryExecutor : IQueryExecutor
{
    private readonly ICacheBackend? _cacheBackend;
    private readonly Func<QueryExpression, Task<OrmQueryResult>> _dbExecutor;
    private readonly IStorageBackend? _storageBackend;
    private readonly IStreamBackend? _streamBackend;

    /// <summary>
    ///     初始化 <see cref="DefaultQueryExecutor" /> 类的新实例
    /// </summary>
    public DefaultQueryExecutor(
        Func<QueryExpression, Task<OrmQueryResult>> dbExecutor,
        ICacheBackend? cacheBackend = null,
        IStorageBackend? storageBackend = null,
        IStreamBackend? streamBackend = null)
    {
        _dbExecutor = dbExecutor;
        _cacheBackend = cacheBackend;
        _storageBackend = storageBackend;
        _streamBackend = streamBackend;
    }

    /// <inheritdoc />
    public async Task<OrmQueryResult> ExecuteAsync(QueryExpression query, CancellationToken ct = default)
    {
        return await _dbExecutor(query);
    }

    /// <inheritdoc />
    public async Task<OrmQueryResult> ExecuteRawAsync(QueryExpression query, CancellationToken ct = default)
    {
        return await _dbExecutor(query);
    }

    /// <inheritdoc />
    public DatabaseQuery<T> Query<T>() where T : class
    {
        return new DatabaseQuery<T>(_dbExecutor);
    }

    /// <inheritdoc />
    public CacheQuery<T> Cache<T>() where T : class
    {
        if (_cacheBackend == null) throw new InvalidOperationException("未配置缓存后端");

        return new CacheQuery<T>(_cacheBackend);
    }

    /// <inheritdoc />
    public StorageQuery<T> Store<T>() where T : class
    {
        if (_storageBackend == null) throw new InvalidOperationException("未配置存储后端");

        return new StorageQuery<T>(_storageBackend);
    }

    /// <inheritdoc />
    public StreamQuery<T> Stream<T>() where T : class
    {
        if (_streamBackend == null) throw new InvalidOperationException("未配置消息流后端");

        return new StreamQuery<T>(_streamBackend);
    }
}