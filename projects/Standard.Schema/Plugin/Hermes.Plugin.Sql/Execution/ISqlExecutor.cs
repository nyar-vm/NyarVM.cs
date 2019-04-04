using System.Data.Common;
using Hermes.Database;
using Hermes.Database.Frontend;

namespace Hermes.Plugin.Sql.Execution;

/// <summary>
///     SQL 执行器接口 — 抽象数据库连接和执行
/// </summary>
public interface ISqlExecutor : IDisposable
{
    /// <summary>
    ///     数据库方言
    /// </summary>
    SqlDialect Dialect { get; }

    /// <summary>
    ///     获取当前连接的数据库名
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    ///     测试数据库连接
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    ///     执行非查询 SQL（DDL/DML）
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, CancellationToken ct = default);

    /// <summary>
    ///     批量执行非查询 SQL
    /// </summary>
    Task<int[]> ExecuteNonQueryBatchAsync(IEnumerable<string> sqlStatements, CancellationToken ct = default);

    /// <summary>
    ///     执行查询 SQL，返回原始 DbDataReader
    /// </summary>
    Task<DbDataReader> ExecuteReaderAsync(string sql, CancellationToken ct = default);

    /// <summary>
    ///     执行查询 SQL，返回标量值
    /// </summary>
    Task<object?> ExecuteScalarAsync(string sql, CancellationToken ct = default);

    /// <summary>
    ///     执行带参数的查询
    /// </summary>
    Task<DbDataReader> ExecuteReaderAsync(string sql, DbParameter[] parameters, CancellationToken ct = default);

    /// <summary>
    ///     执行带参数的非查询
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, DbParameter[] parameters, CancellationToken ct = default);

    /// <summary>
    ///     创建参数
    /// </summary>
    DbParameter CreateParameter(string name, object? value);

    /// <summary>
    ///     开始事务
    /// </summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>
    ///     提交事务
    /// </summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>
    ///     回滚事务
    /// </summary>
    Task RollbackAsync(CancellationToken ct = default);

    /// <summary>
    ///     执行查询并返回结构化的 QueryResult（默认实现基于 ExecuteReaderAsync）
    /// </summary>
    async Task<QueryResult> ExecuteQueryAsync(string sql, CancellationToken ct = default)
    {
        try
        {
            using var reader = await ExecuteReaderAsync(sql, ct);
            var rows = new List<IReadOnlyDictionary<string, object?>>();
            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }

            return QueryResult.Ok(rows);
        }
        catch (Exception ex)
        {
            return QueryResult.Fail(ex.Message);
        }
    }

    /// <summary>
    ///     在事务中执行一组操作，自动处理提交与回滚
    /// </summary>
    async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        await BeginTransactionAsync(ct);
        try
        {
            await action();
            await CommitAsync(ct);
        }
        catch
        {
            await RollbackAsync(ct);
            throw;
        }
    }
}

/// <summary>
///     SQL 执行器工厂 — 使用统一的 <see cref="ConnectionStringParser" /> 解析。
/// </summary>
public static class SqlExecutorFactory
{
    /// <summary>
    ///     根据连接字符串创建执行器（自动推断提供程序类型）。
    /// </summary>
    public static ISqlExecutor Create(string connectionString)
    {
        var provider = ConnectionStringParser.InferProvider(connectionString);

        return provider switch
        {
            DatabaseProvider.MySQL => new MySqlExecutor(connectionString),
            DatabaseProvider.PostgreSQL => new PostgreSqlExecutor(connectionString),
            DatabaseProvider.SQLite => new SqliteExecutor(connectionString),
            _ => throw new ArgumentException(
                $"无法识别的连接字符串格式: {connectionString[..Math.Min(50, connectionString.Length)]}...")
        };
    }

    /// <summary>
    ///     根据方言和连接字符串创建执行器。
    /// </summary>
    public static ISqlExecutor Create(SqlDialect dialect, string connectionString)
    {
        return dialect.Name switch
        {
            "mysql" => new MySqlExecutor(connectionString),
            "postgresql" => new PostgreSqlExecutor(connectionString),
            "sqlite" => new SqliteExecutor(connectionString),
            _ => throw new ArgumentException($"不支持的方言: {dialect.Name}")
        };
    }
}