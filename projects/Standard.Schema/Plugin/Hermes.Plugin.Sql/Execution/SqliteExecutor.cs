using System.Data.Common;
using System.Globalization;
using Hermes.Database.Sqlite;

namespace Hermes.Plugin.Sql.Execution;

/// <summary>
///     SQLite 执行器 — 基于自研 Hermes.Database.Sqlite 协议库
/// </summary>
public sealed class SqliteExecutor : ISqlExecutor
{
    private readonly SqliteConnection _connection;
    private readonly SqliteConnectOptions _options;
    private bool _transactionActive;

    /// <summary>
    ///     初始化 <see cref="SqliteExecutor" /> 类的新实例，从连接字符串解析路径并创建连接。
    /// </summary>
    /// <param name="connectionString">
    ///     SQLite 连接字符串，格式如 "Data Source=:memory:" 或 "Data Source=/path/to/db.sqlite"
    /// </param>
    public SqliteExecutor(string connectionString)
    {
        var databasePath = ParseDatabasePath(connectionString);
        _options = new SqliteConnectOptions { DatabasePath = databasePath };
        _connection = new SqliteConnection(_options);
    }

    /// <summary>
    ///     数据库方言类型
    /// </summary>
    public SqlDialect Dialect => SqlDialect.Sqlite;

    /// <summary>
    ///     当前连接的数据库文件路径
    /// </summary>
    public string DatabaseName => _options.DatabasePath;

    #region 参数

    /// <summary>
    ///     创建一个数据库参数。
    /// </summary>
    public DbParameter CreateParameter(string name, object? value)
    {
        return new HermesDbParameter(name, value);
    }

    #endregion

    #region 生命周期

    /// <summary>
    ///     释放数据库连接资源。
    /// </summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    #endregion

    #region 连接字符串解析

    /// <summary>
    ///     从连接字符串中提取 Data Source 对应的数据库文件路径。
    /// </summary>
    private static string ParseDatabasePath(string connectionString)
    {
        var span = connectionString.AsSpan();
        const string keyword = "Data Source=";

        var index = span.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return ":memory:";

        var valueStart = index + keyword.Length;
        var remaining = span[valueStart..].Trim();

        if (remaining.Length > 0 && remaining[0] == '\"') remaining = remaining[1..];

        if (remaining.Length > 0 && remaining[^1] == '\"') remaining = remaining[..^1];

        if (remaining.Length > 0 && remaining[0] == '\'') remaining = remaining[1..];

        if (remaining.Length > 0 && remaining[^1] == '\'') remaining = remaining[..^1];

        return remaining.Trim().ToString();
    }

    #endregion

    #region 连接管理

    /// <summary>
    ///     测试数据库连接是否可用。
    /// </summary>
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

    /// <summary>
    ///     确保连接处于已打开状态，若未连接则调用 ConnectAsync 建立连接。
    /// </summary>
    private async Task EnsureOpenAsync(CancellationToken ct)
    {
        if (!_connection.IsConnected) await _connection.ConnectAsync(ct);
    }

    #endregion

    #region 非查询执行

    /// <summary>
    ///     执行非查询 SQL（DDL/DML），返回受影响行数。
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);
        return result.AffectedRows;
    }

    /// <summary>
    ///     执行带参数的非查询 SQL，参数会被内联到 SQL 文本中。
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string sql, DbParameter[] parameters, CancellationToken ct = default)
    {
        var inlinedSql = InlineParameters(sql, parameters);
        return await ExecuteNonQueryAsync(inlinedSql, ct);
    }

    /// <summary>
    ///     批量执行非查询 SQL 语句，返回每条语句的受影响行数。所有语句在事务中执行。
    /// </summary>
    public async Task<int[]> ExecuteNonQueryBatchAsync(IEnumerable<string> sqlStatements,
        CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var results = new List<int>();

        await BeginTransactionAsync(ct);

        try
        {
            foreach (var sql in sqlStatements)
            {
                if (string.IsNullOrWhiteSpace(sql)) continue;

                var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);
                results.Add(result.AffectedRows);
            }

            await CommitAsync(ct);
        }
        catch
        {
            await RollbackAsync(ct);
            throw;
        }

        return [.. results];
    }

    #endregion

    #region 查询执行

    /// <summary>
    ///     执行查询 SQL，返回 <see cref="DbDataReader" /> 结果集。
    /// </summary>
    public async Task<DbDataReader> ExecuteReaderAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);
        return new DictionaryDbDataReader(result.Columns, result.Rows);
    }

    /// <summary>
    ///     执行带参数的查询 SQL，参数会被内联后返回 <see cref="DbDataReader" />。
    /// </summary>
    public async Task<DbDataReader> ExecuteReaderAsync(string sql, DbParameter[] parameters,
        CancellationToken ct = default)
    {
        var inlinedSql = InlineParameters(sql, parameters);
        return await ExecuteReaderAsync(inlinedSql, ct);
    }

    /// <summary>
    ///     执行查询 SQL，返回第一行第一列的值。
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryWithRowsAsync(sql, ct);

        if (result.Rows.Count == 0 || result.Columns.Count == 0) return null;

        return result.Rows[0].GetValueOrDefault(result.Columns[0]);
    }

    #endregion

    #region 事务

    /// <summary>
    ///     开始事务，通过执行 BEGIN TRANSACTION SQL 语句。
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transactionActive) return;

        await EnsureOpenAsync(ct);
        await _connection.ExecuteQueryWithRowsAsync("BEGIN TRANSACTION", ct);
        _transactionActive = true;
    }

    /// <summary>
    ///     提交事务，通过执行 COMMIT SQL 语句。
    /// </summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (!_transactionActive) return;

        _transactionActive = false;
        await _connection.ExecuteQueryWithRowsAsync("COMMIT", ct);
    }

    /// <summary>
    ///     回滚事务，通过执行 ROLLBACK SQL 语句。
    /// </summary>
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (!_transactionActive) return;

        _transactionActive = false;
        await _connection.ExecuteQueryWithRowsAsync("ROLLBACK", ct);
    }

    #endregion

    #region 参数内联

    /// <summary>
    ///     将 DbParameter 数组内联到 SQL 文本中，替换 @paramName 占位符为 SQL 字面值。
    /// </summary>
    private static string InlineParameters(string sql, DbParameter[] parameters)
    {
        var result = sql;

        foreach (var p in parameters)
        {
            var paramName = p.ParameterName.StartsWith('@') ? p.ParameterName : $"@{p.ParameterName}";
            var literal = ToSqlLiteral(p.Value);
            result = ReplaceParameter(result, paramName, literal);
        }

        return result;
    }

    /// <summary>
    ///     在 SQL 字符串中替换指定参数名为字面值，仅匹配完整标识符边界。
    /// </summary>
    private static string ReplaceParameter(string sql, string paramName, string literal)
    {
        var index = 0;
        var result = sql;

        while (true)
        {
            index = result.IndexOf(paramName, index, StringComparison.OrdinalIgnoreCase);
            if (index < 0) break;

            var isStartValid = index == 0 || !IsIdentifierChar(result[index - 1]);
            var endPos = index + paramName.Length;
            var isEndValid = endPos >= result.Length || !IsIdentifierChar(result[endPos]);

            if (isStartValid && isEndValid)
            {
                result = result[..index] + literal + result[endPos..];
                index += literal.Length;
            }
            else
            {
                index++;
            }
        }

        return result;
    }

    /// <summary>
    ///     判断字符是否为 SQL 标识符字符（字母、数字、下划线）。
    /// </summary>
    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    /// <summary>
    ///     将参数值转换为 SQL 字面值字符串表示。
    /// </summary>
    private static string ToSqlLiteral(object? value)
    {
        if (value == null || value == DBNull.Value) return "NULL";

        switch (value)
        {
            case string s:
                return $"'{s.Replace("'", "''")}'";
            case char c:
                return $"'{c.ToString().Replace("'", "''")}'";
            case bool b:
                return b ? "1" : "0";
            case byte u8:
                return u8.ToString(CultureInfo.InvariantCulture);
            case sbyte i8:
                return i8.ToString(CultureInfo.InvariantCulture);
            case short i16:
                return i16.ToString(CultureInfo.InvariantCulture);
            case ushort u16:
                return u16.ToString(CultureInfo.InvariantCulture);
            case int i32:
                return i32.ToString(CultureInfo.InvariantCulture);
            case uint u32:
                return u32.ToString(CultureInfo.InvariantCulture);
            case long i64:
                return i64.ToString(CultureInfo.InvariantCulture);
            case ulong u64:
                return u64.ToString(CultureInfo.InvariantCulture);
            case float f:
                return f.ToString("G9", CultureInfo.InvariantCulture);
            case double d:
                return d.ToString("G17", CultureInfo.InvariantCulture);
            case decimal m:
                return m.ToString(CultureInfo.InvariantCulture);
            case DateTime dt:
                return $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
            case DateTimeOffset dto:
                return $"'{dto:yyyy-MM-dd HH:mm:ss.fffffK}'";
            case Guid g:
                return $"'{g:D}'";
            case byte[] bytes:
                return $"X'{Convert.ToHexString(bytes)}'";
            default:
                var str = value.ToString() ?? "";
                return $"'{str.Replace("'", "''")}'";
        }
    }

    #endregion
}