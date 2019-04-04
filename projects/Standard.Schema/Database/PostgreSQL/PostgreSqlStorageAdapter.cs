using Hermes.Database.Frontend;
using Hermes.Database.Schema;
using Hermes.YYDB.Query;

namespace Hermes.Database.PostgreSql;

public sealed class PostgreSqlStorageAdapter : IStorageAdapter, IDisposable
{
    private readonly PostgreSqlConnection _connection;
    private readonly PostgreSqlQueryTranslator _translator;
    private bool _disposed;

    public PostgreSqlStorageAdapter(PostgreSqlConnectOptions options, string? name = null)
    {
        Name = name ?? $"postgresql-{options.Host}:{options.Port}";
        _connection = new PostgreSqlConnection(options);
        _translator = new PostgreSqlQueryTranslator();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _connection.Dispose();
    }

    public StorageBackendKind Kind => StorageBackendKind.PostgreSQL;

    public string Name { get; }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_connection.IsConnected) await _connection.ConnectAsync(ct);

            var result = await _connection.ExecuteQueryAsync("SELECT 1 AS alive", ct);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<QueryResult> ReadAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"PostgreSQL 查询失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.Rows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    public async Task<QueryResult> WriteAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"PostgreSQL 写入失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.AffectedRows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    public async Task<QueryResult> DeleteAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"PostgreSQL 删除失败：{result.ErrorMessage}");

            return QueryResult.Ok(result.AffectedRows, Name);
        }
        catch (InvalidOperationException ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    public async Task<QueryResult> AggregateAsync(QueryExpression query, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await EnsureConnectedAsync(ct);

            var sql = _translator.Translate(query);
            var result = await _connection.ExecuteQueryAsync(sql, ct);

            if (!result.Success) return QueryResult.Fail($"PostgreSQL 聚合失败：{result.ErrorMessage}");

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