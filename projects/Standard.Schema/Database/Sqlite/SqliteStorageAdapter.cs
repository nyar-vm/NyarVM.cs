using Hermes.Database.Frontend;
using Hermes.Database.Schema;
using Hermes.YYDB.Query;

namespace Hermes.Database.Sqlite;

/// <summary>
///     SQLite 存储适配器，实现 <see cref="IStorageAdapter" /> 接口。
/// </summary>
public sealed class SqliteStorageAdapter : IStorageAdapter, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteQueryTranslator _translator;
    private bool _disposed;

    /// <summary>
    ///     初始化 <see cref="SqliteStorageAdapter" /> 类的新实例。
    /// </summary>
    /// <param name="options">SQLite 连接选项。</param>
    /// <param name="name">适配器名称（可选）。</param>
    public SqliteStorageAdapter(SqliteConnectOptions options, string? name = null)
    {
        Name = name ?? $"sqlite-{Path.GetFileName(options.DatabasePath)}";
        _connection = new SqliteConnection(options);
        _translator = new SqliteQueryTranslator();
    }

    /// <summary>
    ///     释放资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _connection.Dispose();
    }

    /// <inheritdoc />
    public StorageBackendKind Kind => StorageBackendKind.SQLite;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_connection.IsConnected) await _connection.ConnectAsync(ct);

            var result = await _connection.ExecuteQueryWithRowsAsync("SELECT 1 AS alive", ct);

            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<QueryResult> ReadAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"SQLite 查询失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.Rows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<QueryResult> WriteAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"SQLite 写入失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.AffectedRows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<QueryResult> DeleteAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"SQLite 删除失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.AffectedRows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<QueryResult> AggregateAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"SQLite 聚合失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.Rows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (!_connection.IsConnected) await _connection.ConnectAsync(ct);
    }
}