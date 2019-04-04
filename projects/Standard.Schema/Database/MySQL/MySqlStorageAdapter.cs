using System.Collections.Concurrent;
using Hermes.Database.Frontend;
using Hermes.Database.Schema;
using Hermes.YYDB.Query;

namespace Hermes.Database.MySql;

/// <summary>
///     MySQL 存储适配器——支持连接池和预编译语句缓存
/// </summary>
public sealed class MySqlStorageAdapter : IStorageAdapter, IDisposable
{
    private readonly IConnectionPool<MySqlConnection> _connectionPool;
    private readonly ConcurrentDictionary<string, PreparedStatementCacheEntry> _preparedStatements;
    private readonly MySqlQueryTranslator _translator;
    private bool _disposed;

    /// <summary>
    ///     创建 MySQL 存储适配器（使用连接池）
    /// </summary>
    /// <param name="connectionPool">MySQL 连接池</param>
    /// <param name="name">适配器名称</param>
    public MySqlStorageAdapter(IConnectionPool<MySqlConnection> connectionPool, string? name = null)
    {
        _connectionPool = connectionPool;
        _translator = new MySqlQueryTranslator();
        _preparedStatements = new ConcurrentDictionary<string, PreparedStatementCacheEntry>();
        Name = name ?? "mysql-pooled";
    }

    /// <summary>
    ///     创建 MySQL 存储适配器（使用单个连接，向后兼容）
    /// </summary>
    /// <param name="options">MySQL 连接选项</param>
    /// <param name="name">适配器名称</param>
    public MySqlStorageAdapter(MySqlConnectOptions options, string? name = null)
    {
        var pool = new DefaultConnectionPool<MySqlConnection>(
            async ct =>
            {
                var conn = new MySqlConnection(options);
                await conn.ConnectAsync(ct);
                return conn;
            },
            new ConnectionPoolOptions { MaxPoolSize = 10, IdleTimeoutSeconds = 300 });

        _connectionPool = pool;
        _translator = new MySqlQueryTranslator();
        _preparedStatements = new ConcurrentDictionary<string, PreparedStatementCacheEntry>();
        Name = name ?? $"mysql-{options.Host}:{options.Port}";
    }

    /// <summary>
    ///     预编译语句缓存条目数
    /// </summary>
    public int PreparedStatementCount => _preparedStatements.Count;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _preparedStatements.Clear();
        _connectionPool.Dispose();
    }

    /// <summary>
    ///     存储后端类型
    /// </summary>
    public StorageBackendKind Kind => StorageBackendKind.MySQL;

    /// <summary>
    ///     适配器名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     检查 MySQL 是否可用
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            await using var pooled = await _connectionPool.RentAsync(ct);
            var result = await pooled.Connection.ExecuteQueryWithRowsAsync("SELECT 1 AS alive", ct);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     读取数据
    /// </summary>
    public async Task<QueryResult> ReadAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await using var pooled = await _connectionPool.RentAsync(ct);
            var sql = _translator.Translate(query);
            var result = await pooled.Connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"MySQL 查询失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.Rows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <summary>
    ///     写入数据（支持批量插入）
    /// </summary>
    public async Task<QueryResult> WriteAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await using var pooled = await _connectionPool.RentAsync(ct);
            var sql = _translator.Translate(query);

            if (query is BatchInsertQuery)
            {
                var totalAffected = 0;
                var statements = sql.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var stmt in statements)
                {
                    var trimmed = stmt.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var result = await pooled.Connection.ExecuteQueryWithRowsAsync(trimmed, ct);
                    if (!result.Success) return QueryResult.Fail($"MySQL 批量写入失败：{result.ErrorMessage}");

                    totalAffected += result.AffectedRows;
                }

                return QueryResult.Ok(totalAffected, Name);
            }

            var singleResult = await pooled.Connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!singleResult.Success) return QueryResult.Fail($"MySQL 写入失败：{singleResult.ErrorMessage}");

            return QueryResult.Ok(singleResult.AffectedRows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <summary>
    ///     删除数据
    /// </summary>
    public async Task<QueryResult> DeleteAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await using var pooled = await _connectionPool.RentAsync(ct);
            var sql = _translator.Translate(query);
            var result = await pooled.Connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"MySQL 删除失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.AffectedRows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <summary>
    ///     聚合查询
    /// </summary>
    public async Task<QueryResult> AggregateAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await using var pooled = await _connectionPool.RentAsync(ct);
            var sql = _translator.Translate(query);
            var result = await pooled.Connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"MySQL 聚合失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.Rows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <summary>
    ///     执行预编译语句——频繁执行的查询自动缓存 SQL 模板
    /// </summary>
    public async Task<QueryResult> ExecutePreparedAsync(string sqlKey, QueryExpression query,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var entry = _preparedStatements.GetOrAdd(sqlKey,
                _ => new PreparedStatementCacheEntry(_translator.Translate(query)));
            entry.IncrementHitCount();

            await using var pooled = await _connectionPool.RentAsync(ct);
            var result = await pooled.Connection.ExecuteQueryWithRowsAsync(entry.Sql, ct);

            if (!result.Success) return QueryResult.Fail($"MySQL 预编译查询失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.Rows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <summary>
    ///     清除预编译语句缓存
    /// </summary>
    public void ClearPreparedStatementCache()
    {
        _preparedStatements.Clear();
    }

    /// <summary>
    ///     预编译语句缓存条目
    /// </summary>
    private sealed class PreparedStatementCacheEntry
    {
        /// <summary>
        ///     命中次数
        /// </summary>
        private int _hitCount;

        public PreparedStatementCacheEntry(string sql)
        {
            Sql = sql;
        }

        /// <summary>
        ///     预编译的 SQL 语句
        /// </summary>
        public string Sql { get; }

        /// <summary>
        ///     获取命中次数
        /// </summary>
        public int HitCount => _hitCount;

        public void IncrementHitCount()
        {
            Interlocked.Increment(ref _hitCount);
        }
    }
}