using System.Data.Common;
using System.Globalization;
using Hermes.Database.PostgreSql;

namespace Hermes.Plugin.Sql.Execution;

/// <summary>
///     PostgreSQL 执行器 — 基于自研 Hermes.Database.PostgreSql 协议库
/// </summary>
public sealed class PostgreSqlExecutor : ISqlExecutor
{
    private readonly PostgreSqlConnection _connection;
    private readonly PostgreSqlConnectOptions _options;
    private bool _transactionActive;

    public PostgreSqlExecutor(string connectionString)
    {
        _options = ParseConnectionString(connectionString);
        _connection = new PostgreSqlConnection(_options);
    }

    public SqlDialect Dialect => SqlDialect.PostgreSql;

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
        var result = await _connection.ExecuteQueryAsync(sql, ct);
        return result.Success ? result.AffectedRows : 0;
    }

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

                var result = await _connection.ExecuteQueryAsync(sql, ct);
                results.Add(result.Success ? result.AffectedRows : 0);
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

    public async Task<DbDataReader> ExecuteReaderAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryAsync(sql, ct);
        return new DictionaryDbDataReader(result.Columns, result.Rows);
    }

    public async Task<object?> ExecuteScalarAsync(string sql, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var result = await _connection.ExecuteQueryAsync(sql, ct);

        if (result.Rows.Count > 0 && result.Columns.Count > 0)
        {
            var firstColumn = result.Columns[0];
            return result.Rows[0][firstColumn];
        }

        return null;
    }

    public async Task<DbDataReader> ExecuteReaderAsync(string sql, DbParameter[] parameters,
        CancellationToken ct = default)
    {
        var inlinedSql = InlineParameters(sql, parameters);
        return await ExecuteReaderAsync(inlinedSql, ct);
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, DbParameter[] parameters, CancellationToken ct = default)
    {
        var inlinedSql = InlineParameters(sql, parameters);
        return await ExecuteNonQueryAsync(inlinedSql, ct);
    }

    public DbParameter CreateParameter(string name, object? value)
    {
        return new HermesDbParameter(name, value);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transactionActive) return;

        await EnsureOpenAsync(ct);
        await _connection.ExecuteQueryAsync("BEGIN", ct);
        _transactionActive = true;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (!_transactionActive) return;

        _transactionActive = false;
        await _connection.ExecuteQueryAsync("COMMIT", ct);
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (!_transactionActive) return;

        _transactionActive = false;
        await _connection.ExecuteQueryAsync("ROLLBACK", ct);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private async Task EnsureOpenAsync(CancellationToken ct = default)
    {
        if (!_connection.IsConnected) await _connection.ConnectAsync(ct);
    }

    private static PostgreSqlConnectOptions ParseConnectionString(string connectionString)
    {
        var pairs = connectionString.Split(';')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s => s.Split('=', 2))
            .Where(a => a.Length == 2)
            .ToDictionary(a => a[0].Trim().ToLowerInvariant(), a => a[1].Trim());

        var host = "localhost";
        var port = 5432;
        var database = "postgres";
        var username = "postgres";
        var password = "";
        var applicationName = "Hermes.Database.PostgreSql";
        var connectionTimeoutSeconds = 30;

        if (pairs.TryGetValue("host", out var hostVal))
            host = hostVal;
        else if (pairs.TryGetValue("server", out var serverVal)) host = serverVal;

        if (pairs.TryGetValue("port", out var portStr) && int.TryParse(portStr, out var parsedPort)) port = parsedPort;

        if (pairs.TryGetValue("database", out var dbVal))
            database = dbVal;
        else if (pairs.TryGetValue("db", out var dbAlias)) database = dbAlias;

        if (pairs.TryGetValue("username", out var userVal))
            username = userVal;
        else if (pairs.TryGetValue("user", out var userAlias))
            username = userAlias;
        else if (pairs.TryGetValue("user id", out var userId)) username = userId;

        if (pairs.TryGetValue("password", out var passVal))
            password = passVal;
        else if (pairs.TryGetValue("pwd", out var pwdVal)) password = pwdVal;

        if (pairs.TryGetValue("application name", out var appName)) applicationName = appName;

        if (pairs.TryGetValue("connection timeout", out var timeoutStr) && int.TryParse(timeoutStr, out var timeout))
            connectionTimeoutSeconds = timeout;

        return new PostgreSqlConnectOptions
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            ApplicationName = applicationName,
            ConnectionTimeoutSeconds = connectionTimeoutSeconds
        };
    }

    private static string InlineParameters(string sql, DbParameter[] parameters)
    {
        foreach (var p in parameters)
        {
            var paramName = "@" + p.ParameterName.TrimStart('@');
            var value = FormatSqlValue(p.Value);
            sql = sql.Replace(paramName, value);
        }

        return sql;
    }

    private static string FormatSqlValue(object? value)
    {
        if (value is null or DBNull) return "NULL";

        switch (value)
        {
            case string s:
                return "'" + s.Replace("'", "''") + "'";

            case bool b:
                return b ? "TRUE" : "FALSE";

            case int i:
                return i.ToString(CultureInfo.InvariantCulture);

            case long l:
                return l.ToString(CultureInfo.InvariantCulture);

            case short sh:
                return sh.ToString(CultureInfo.InvariantCulture);

            case byte by:
                return by.ToString(CultureInfo.InvariantCulture);

            case float f:
                return f.ToString("R", CultureInfo.InvariantCulture);

            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture);

            case decimal m:
                return m.ToString(CultureInfo.InvariantCulture);

            case DateTime dt:
                return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture) + "'";

            case Guid g:
                return "'" + g.ToString("D") + "'";

            case byte[] bytes:
                return "'\\x" + Convert.ToHexString(bytes) + "'";

            default:
                return "'" + value.ToString()!.Replace("'", "''") + "'";
        }
    }
}