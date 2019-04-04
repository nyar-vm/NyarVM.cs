namespace Hermes.Database;

/// <summary>
///     连接字符串解析器，统一 URI 和键值对格式的数据库连接字符串解析。
/// </summary>
public static class ConnectionStringParser
{
    /// <summary>
    ///     从 URI 或键值对格式的连接字符串中解析数据库提供程序类型。
    /// </summary>
    /// <param name="connectionString">连接字符串（支持 URI 格式或键值对格式）。</param>
    /// <returns>数据库提供程序类型。</returns>
    public static DatabaseProvider InferProvider(string connectionString)
    {
        if (TryParseUri(connectionString, out var provider, out _)) return provider;

        return InferProviderFromKeyValue(connectionString);
    }

    /// <summary>
    ///     尝试将 URI 格式的连接字符串转换为提供程序特定的键值对连接字符串。
    ///     如果已是键值对格式，则原样返回。
    /// </summary>
    /// <param name="connectionString">连接字符串（支持 URI 格式或键值对格式）。</param>
    /// <param name="provider">输出：识别到的数据库提供程序类型。</param>
    /// <param name="resultConnectionString">输出：提供程序特定的键值对连接字符串。</param>
    /// <returns>是否成功解析。</returns>
    public static bool TryParse(string connectionString, out DatabaseProvider provider,
        out string resultConnectionString)
    {
        if (TryParseUri(connectionString, out provider, out resultConnectionString)) return true;

        provider = InferProviderFromKeyValue(connectionString);
        resultConnectionString = connectionString;

        return provider != DatabaseProvider.Unknown;
    }

    /// <summary>
    ///     尝试解析 URI 格式的连接字符串。
    /// </summary>
    /// <param name="url">URI 格式的数据库连接地址。</param>
    /// <param name="provider">输出：数据库提供程序类型。</param>
    /// <param name="connectionString">输出：提供程序特定的连接字符串。</param>
    /// <returns>是否成功解析。</returns>
    public static bool TryParseUri(string url, out DatabaseProvider provider, out string connectionString)
    {
        provider = DatabaseProvider.Unknown;
        connectionString = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;

        return TryParseUri(uri, out provider, out connectionString);
    }

    /// <summary>
    ///     从 <see cref="Uri" /> 解析数据库连接信息。
    /// </summary>
    /// <param name="uri">数据库连接 URI。</param>
    /// <param name="provider">输出：数据库提供程序类型。</param>
    /// <param name="connectionString">输出：提供程序特定的连接字符串。</param>
    /// <returns>是否成功解析。</returns>
    public static bool TryParseUri(Uri uri, out DatabaseProvider provider, out string connectionString)
    {
        provider = DatabaseProvider.Unknown;
        connectionString = string.Empty;

        var scheme = uri.Scheme.ToLowerInvariant();

        switch (scheme)
        {
            case "postgresql" or "pgsql":
                provider = DatabaseProvider.PostgreSQL;
                connectionString = BuildPostgreSqlConnectionString(uri);
                return true;

            case "mysql":
                provider = DatabaseProvider.MySQL;
                connectionString = BuildMySqlConnectionString(uri);
                return true;

            case "sqlite":
                provider = DatabaseProvider.SQLite;
                connectionString = $"Data Source={uri.AbsolutePath}";
                return true;

            default:
                return false;
        }
    }

    private static DatabaseProvider InferProviderFromKeyValue(string connectionString)
    {
        var lower = connectionString.ToLowerInvariant();

        if (lower.Contains("mysql")) return DatabaseProvider.MySQL;

        if (lower.Contains("postgres") || lower.Contains("pgsql")) return DatabaseProvider.PostgreSQL;

        if (lower.Contains("sqlite") || lower.Contains(".db") || lower.StartsWith("data source="))
            return DatabaseProvider.SQLite;

        return DatabaseProvider.Unknown;
    }

    private static string BuildPostgreSqlConnectionString(Uri uri)
    {
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var username = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[0] : uri.UserInfo;
        var password = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : string.Empty;

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    private static string BuildMySqlConnectionString(Uri uri)
    {
        var server = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 3306;
        var database = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':') : [uri.UserInfo, string.Empty];
        var userId = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;

        return $"Server={server};Port={port};Database={database};User ID={userId};Password={password}";
    }
}

/// <summary>
///     数据库提供程序类型。
/// </summary>
public enum DatabaseProvider
{
    /// <summary>
    ///     未知或未识别类型。
    /// </summary>
    Unknown,

    /// <summary>
    ///     MySQL / MariaDB。
    /// </summary>
    MySQL,

    /// <summary>
    ///     PostgreSQL。
    /// </summary>
    PostgreSQL,

    /// <summary>
    ///     SQLite。
    /// </summary>
    SQLite,

    /// <summary>
    ///     内存数据库（用于测试）。
    /// </summary>
    InMemory
}