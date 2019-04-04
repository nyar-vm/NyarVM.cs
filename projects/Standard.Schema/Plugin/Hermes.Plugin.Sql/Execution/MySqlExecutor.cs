using System.Data.Common;
using Hermes.Database.MySql;

namespace Hermes.Plugin.Sql.Execution;

/// <summary>
///     MySQL 执行器 — 基于 Hermes.Database.MySql（自研协议库实现）
/// </summary>
public sealed class MySqlExecutor : ISqlExecutor
{
    private readonly MySqlConnection _connection;
    private readonly MySqlConnectOptions _options;

    public MySqlExecutor(string connectionString)
    {
        _options = ParseConnectionString(connectionString);
        _connection = new MySqlConnection(_options);
    }

    public SqlDialect Dialect => SqlDialect.MySql;

    public string DatabaseName => _options.Database;

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _connection.ConnectAsync();
            await _connection.CloseAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var packet = await _connection.ExecuteQueryAsync(sql, ct);
        return TryExtractAffectedRows(packet);
    }

    public async Task<int[]> ExecuteNonQueryBatchAsync(IEnumerable<string> sqlStatements,
        CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var results = new List<int>();

        foreach (var sql in sqlStatements)
        {
            if (string.IsNullOrWhiteSpace(sql)) continue;

            var packet = await _connection.ExecuteQueryAsync(sql, ct);
            results.Add(TryExtractAffectedRows(packet));
        }

        return [.. results];
    }

    public async Task<DbDataReader> ExecuteReaderAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);

        if (!result.Success) throw new InvalidOperationException($"MySQL 查询失败：{result.ErrorMessage}");

        return new DictionaryDbDataReader(result.Columns, result.Rows);
    }

    public async Task<object?> ExecuteScalarAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);

        if (!result.Success) throw new InvalidOperationException($"MySQL 查询失败：{result.ErrorMessage}");

        if (result.Rows.Count == 0 || result.Columns.Count == 0) return null;

        return result.Rows[0].GetValueOrDefault(result.Columns[0]);
    }

    public async Task<DbDataReader> ExecuteReaderAsync(string sql, DbParameter[] parameters,
        CancellationToken ct = default)
    {
        var inlinedSql = InlineParameters(sql, parameters);
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryWithRowsAsync(inlinedSql, ct);

        if (!result.Success) throw new InvalidOperationException($"MySQL 查询失败：{result.ErrorMessage}");

        return new DictionaryDbDataReader(result.Columns, result.Rows);
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, DbParameter[] parameters, CancellationToken ct = default)
    {
        var inlinedSql = InlineParameters(sql, parameters);
        await EnsureOpenAsync(ct);
        var packet = await _connection.ExecuteQueryAsync(inlinedSql, ct);
        return TryExtractAffectedRows(packet);
    }

    public DbParameter CreateParameter(string name, object? value)
    {
        return new HermesDbParameter(name, value);
    }

    public Task BeginTransactionAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private async Task EnsureOpenAsync(CancellationToken ct = default)
    {
        if (!_connection.IsConnected) await _connection.ConnectAsync(ct);
    }

    /// <summary>
    ///     解析键值对格式的连接字符串为 <see cref="MySqlConnectOptions" />。
    /// </summary>
    private static MySqlConnectOptions ParseConnectionString(string connectionString)
    {
        var pairs = connectionString.Split(';')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s => s.Split('=', 2))
            .Where(a => a.Length == 2)
            .ToDictionary(a => a[0].Trim().ToLowerInvariant(), a => a[1].Trim());

        return new MySqlConnectOptions
        {
            Host = pairs.GetValueOrDefault("server") ?? pairs.GetValueOrDefault("host") ?? "localhost",
            Port = int.TryParse(pairs.GetValueOrDefault("port") ?? "3306", out var port) ? port : 3306,
            Username = pairs.GetValueOrDefault("user id") ?? pairs.GetValueOrDefault("username") ?? "root",
            Password = pairs.GetValueOrDefault("password") ?? pairs.GetValueOrDefault("pwd") ?? "",
            Database = pairs.GetValueOrDefault("database") ?? pairs.GetValueOrDefault("initial catalog") ?? "",
            Charset = pairs.GetValueOrDefault("character set") ?? pairs.GetValueOrDefault("charset") ?? "utf8mb4"
        };
    }

    /// <summary>
    ///     将 <see cref="DbParameter" /> 数组内联到 SQL 语句中，替换 @参数名 为参数值的字符串表示。
    /// </summary>
    private static string InlineParameters(string sql, DbParameter[] parameters)
    {
        var result = sql;

        foreach (var p in parameters)
        {
            var paramName = p.ParameterName.StartsWith('@') ? p.ParameterName : $"@{p.ParameterName}";
            var valueStr = FormatParameterValue(p.Value);
            result = result.Replace(paramName, valueStr);
        }

        return result;
    }

    /// <summary>
    ///     将参数值格式化为 SQL 字面量字符串。
    /// </summary>
    private static string FormatParameterValue(object? value)
    {
        if (value is null || value == DBNull.Value) return "NULL";

        if (value is string s) return $"'{s.Replace("'", "''")}'";

        if (value is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss}'";

        if (value is bool b) return b ? "1" : "0";

        return value.ToString() ?? "NULL";
    }

    /// <summary>
    ///     尝试从 OK 包中提取受影响行数，解析失败时返回 -1。
    /// </summary>
    private static int TryExtractAffectedRows(MySqlPacketData packet)
    {
        var data = packet.Data;

        if (data == null || data.Length < 2) return -1;

        if (data[0] != 0x00) return -1;

        var buffer = new ByteBuffer(data);
        buffer.ReadU8();

        return (int)ReadLengthEncodedInteger(ref buffer);
    }

    /// <summary>
    ///     从缓冲区读取长度编码整数（MySQL 协议）。
    /// </summary>
    private static ulong ReadLengthEncodedInteger(ref ByteBuffer buffer)
    {
        var firstByte = buffer.ReadU8();

        if (firstByte < 0xFB) return firstByte;

        if (firstByte == 0xFB) return 0;

        if (firstByte == 0xFC) return buffer.ReadU16LE();

        if (firstByte == 0xFD) return buffer.ReadU32LE();

        return buffer.ReadU64LE();
    }
}