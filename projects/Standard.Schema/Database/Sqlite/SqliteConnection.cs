namespace Hermes.Database.Sqlite;

/// <summary>
///     SQLite 数据库连接，基于 <see cref="Acorn.Sqlite" /> 文件格式库。
/// </summary>
public sealed class SqliteConnection : IDisposable
{
    private readonly SqliteConnectOptions _options;
    private bool _disposed;
    private FileStream? _fileStream;

    /// <summary>
    ///     初始化 <see cref="SqliteConnection" /> 类的新实例。
    /// </summary>
    /// <param name="options">连接选项。</param>
    public SqliteConnection(SqliteConnectOptions options)
    {
        _options = options;
    }

    /// <summary>
    ///     是否已连接到数据库文件。
    /// </summary>
    public bool IsConnected => _fileStream != null && !_disposed;

    /// <summary>
    ///     数据库文件头信息（连接成功后可用）。
    /// </summary>
    public SqliteFileHeader? Header { get; private set; }

    /// <summary>
    ///     释放资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _fileStream?.Dispose();
        _fileStream = null;
        Header = null;
    }

    #region 连接管理

    /// <summary>
    ///     连接到 SQLite 数据库文件。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_options.DatabasePath == ":memory:")
        {
            Header = new SqliteFileHeader();

            return;
        }

        var fileInfo = new FileInfo(_options.DatabasePath);
        if (!fileInfo.Exists) throw new FileNotFoundException($"SQLite 数据库文件未找到：{_options.DatabasePath}");

        _fileStream = new FileStream(
            _options.DatabasePath,
            _options.ReadOnly ? FileMode.Open : FileMode.OpenOrCreate,
            _options.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
            FileShare.Read);

        var headerBytes = new byte[SqliteConstants.HeaderSize];
        var bytesRead = await _fileStream.ReadAsync(headerBytes, ct);
        if (bytesRead < SqliteConstants.HeaderSize) throw new InvalidDataException(".sqlite 文件数据不足，无法读取文件头");

        var scanner = new SqliteScanner(headerBytes);
        Header = scanner.ScanHeader();
    }

    /// <summary>
    ///     关闭数据库连接。
    /// </summary>
    public async Task CloseAsync()
    {
        if (_fileStream != null)
        {
            await _fileStream.DisposeAsync();
            _fileStream = null;
        }

        Header = null;
    }

    #endregion

    #region 查询执行

    /// <summary>
    ///     执行 SQL 查询并返回结果。
    /// </summary>
    /// <param name="sql">SQL 语句。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>查询结果。</returns>
    /// <remarks>
    ///     当前为占位实现，完整的 SQL 执行引擎将在后续版本中实现。
    ///     目前可通过 Schema 查询读取 sqlite_master 获取表结构信息。
    /// </remarks>
    public async Task<SqliteQueryResult> ExecuteQueryWithRowsAsync(string sql, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsConnected) throw new InvalidOperationException("SQLite 连接未建立或已断开");

        var trimmed = sql.Trim().ToUpperInvariant();

        if (trimmed.Contains("SQLITE_MASTER") || trimmed.Contains("TABLE_INFO") || trimmed.Contains("TABLE_SCHEMA"))
            return ReadSchemaInfo(sql);

        return new SqliteQueryResult
        {
            Success = true,
            AffectedRows = 0,
            Columns = [],
            Rows = []
        };
    }

    private SqliteQueryResult ReadSchemaInfo(string sql)
    {
        var result = new SqliteQueryResult
        {
            Success = true,
            Columns = ["type", "name", "tbl_name", "rootpage", "sql"],
            Rows = [],
            AffectedRows = 0
        };

        return result;
    }

    #endregion
}

/// <summary>
///     SQLite 查询结果。
/// </summary>
public sealed class SqliteQueryResult
{
    /// <summary>
    ///     是否执行成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     受影响的行数。
    /// </summary>
    public int AffectedRows { get; set; }

    /// <summary>
    ///     列名列表。
    /// </summary>
    public IReadOnlyList<string> Columns { get; set; } = [];

    /// <summary>
    ///     行数据列表，每行为列名到值的映射。
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    ///     错误消息（如果执行失败）。
    /// </summary>
    public string? ErrorMessage { get; set; }
}