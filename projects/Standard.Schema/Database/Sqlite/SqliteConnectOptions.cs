namespace Hermes.Database.Sqlite;

/// <summary>
///     SQLite 数据库连接选项。
/// </summary>
public sealed class SqliteConnectOptions
{
    /// <summary>
    ///     数据库文件路径。
    /// </summary>
    public string DatabasePath { get; init; } = ":memory:";

    /// <summary>
    ///     连接超时时间（秒）。
    /// </summary>
    public int ConnectionTimeoutSeconds { get; init; } = 30;

    /// <summary>
    ///     是否为只读模式。
    /// </summary>
    public bool ReadOnly { get; init; }
}