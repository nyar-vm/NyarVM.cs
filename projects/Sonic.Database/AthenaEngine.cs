using Olympus.Athena.Cache;
using Olympus.Athena.Core;
using Olympus.Athena.Index;
using Olympus.Athena.Query;
using Olympus.Athena.Storage;
using Olympus.Athena.Udf;
using Std.Data.Text.Sql;

namespace Olympus.Athena;

#region AthenaEngine 嵌入式数据库引擎

/// <summary>
///     Athena 嵌入式数据库引擎，对标 DuckDB 的查询执行、微分区存储和索引管理，实现 <see cref="IAsyncDisposable" />
/// </summary>
public sealed class AthenaEngine : IAsyncDisposable
{
    #region 构造函数

    private AthenaEngine(AthenaOptions options)
    {
        _options = options;
        _tables = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase);
        _vectorIndexes = new Dictionary<string, IVectorIndex>(StringComparer.OrdinalIgnoreCase);
        _graphIndexes = new Dictionary<string, IGraphIndex>(StringComparer.OrdinalIgnoreCase);
        _invertedIndexes = new Dictionary<string, IInvertedIndex>(StringComparer.OrdinalIgnoreCase);
        _writer = new MicroPartitionWriter(options.DataDirectory);
        _reader = new MicroPartitionReader(options.DataDirectory);
        _cache = new CacheManager(Path.Combine(options.DataDirectory, "cache"), options.MemoryCacheBytes);
        _udfRegistry = new UdfRegistry();
        _tablePartitions = new Dictionary<string, List<UUIDv7>>(StringComparer.OrdinalIgnoreCase);
        _pendingRows = new Dictionary<string, List<DataValue[]>>(StringComparer.OrdinalIgnoreCase);
        _writeLock = new SemaphoreSlim(1, 1);
        _queryEngine = new QueryEngine(_tables, _cache, _reader);

        Directory.CreateDirectory(options.DataDirectory);
    }

    #endregion

    #region IAsyncDisposable

    /// <summary>
    ///     释放 Athena 引擎占用的所有资源，在关闭前会自动刷盘所有未写入的缓冲行
    /// </summary>
    /// <returns>释放任务</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        await _writeLock.WaitAsync();
        try
        {
            foreach (var (tableName, pending) in _pendingRows)
                if (pending.Count > 0 && _tables.TryGetValue(tableName, out var definition))
                    await FlushRowsAsync(tableName, definition, pending, CancellationToken.None);

            _pendingRows.Clear();
        }
        finally
        {
            _writeLock.Release();
        }

        _writeLock.Dispose();
        _cache.Dispose();
        _tablePartitions.Clear();
        _tables.Clear();
        _vectorIndexes.Clear();
        _graphIndexes.Clear();
        _invertedIndexes.Clear();
    }

    #endregion

    #region 静态工厂方法

    /// <summary>
    ///     打开或创建 Athena 数据库实例
    /// </summary>
    /// <param name="path">数据存储目录路径</param>
    /// <param name="options">可选的自定义配置，为 <c>null</c> 时使用默认配置</param>
    /// <returns>Athena 引擎实例</returns>
    public static AthenaEngine Open(string path, AthenaOptions? options = null)
    {
        var resolvedOptions = options ?? AthenaOptions.Default;

        var merged = new AthenaOptions(
            path,
            resolvedOptions.MemoryCacheBytes,
            resolvedOptions.MicroPartitionRows,
            resolvedOptions.DefaultIndexType,
            resolvedOptions.EnableDiskCache);

        return new AthenaEngine(merged);
    }

    #endregion

    #region 表操作

    /// <summary>
    ///     创建表，使用指定的表定义注册模式
    /// </summary>
    /// <param name="definition">表定义，包含表名和列模式</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>创建任务</returns>
    public Task CreateTableAsync(TableDefinition definition, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_tables.TryAdd(definition.Name, definition)) throw new AthenaException($"表 '{definition.Name}' 已存在");

        _pendingRows[definition.Name] = [];
        _tablePartitions[definition.Name] = [];

        return Task.CompletedTask;
    }

    #endregion

    #region 查询执行

    /// <summary>
    ///     执行 SQL 查询，以异步流方式返回结果行
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>查询结果的异步可枚举流</returns>
    public IAsyncEnumerable<DataValue[]> ExecuteAsync(string sql, CancellationToken ct = default)
    {
        var parser = new SqlParser();
        var result = parser.parse(sql);
        if (!result.IsSuccess || result.value is not SelectStatement)
            throw new AthenaException("SQL 解析失败或不是 SELECT 语句");

        var binder = new QueryBinder(_tables);
        var bound = binder.Bind(result.value);

        return _queryEngine.ExecuteAsync(sql, ct);
    }

    #endregion

    #region 内部辅助方法

    private async Task FlushRowsAsync(string tableName, TableDefinition definition, List<DataValue[]> rows,
        CancellationToken ct)
    {
        var partitionId = UUIDv7.New();
        var schema = definition.Schema;

        using var partition = new MicroPartition(partitionId, schema);

        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var row = rows[rowIdx];
            for (var colIdx = 0; colIdx < schema.Count; colIdx++) partition.AppendRow(colIdx, row[colIdx]);
        }

        await _writer.WriteAsync(partition, ct);
        _tablePartitions[tableName].Add(partitionId);
    }

    #endregion

    #region 内部字段

    private readonly AthenaOptions _options;
    private readonly Dictionary<string, TableDefinition> _tables;
    private readonly Dictionary<string, IVectorIndex> _vectorIndexes;
    private readonly Dictionary<string, IGraphIndex> _graphIndexes;
    private readonly Dictionary<string, IInvertedIndex> _invertedIndexes;
    private readonly MicroPartitionWriter _writer;
    private readonly MicroPartitionReader _reader;
    private readonly CacheManager _cache;
    private readonly UdfRegistry _udfRegistry;
    private readonly Dictionary<string, List<UUIDv7>> _tablePartitions;
    private readonly Dictionary<string, List<DataValue[]>> _pendingRows;
    private readonly SemaphoreSlim _writeLock;
    private readonly QueryEngine _queryEngine;
    private bool _disposed;

    #endregion

    #region 数据写入

    /// <summary>
    ///     向指定表插入单行数据，当缓冲行数达到 <see cref="AthenaOptions.MicroPartitionRows" /> 时自动刷盘
    /// </summary>
    /// <param name="tableName">目标表名</param>
    /// <param name="row">行数据，数组长度必须与表模式列数一致</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>插入任务</returns>
    public async Task InsertAsync(string tableName, DataValue[] row, CancellationToken ct = default)
    {
        await InsertBatchAsync(tableName, [row], ct);
    }

    /// <summary>
    ///     向指定表批量插入多行数据，当缓冲行数达到 <see cref="AthenaOptions.MicroPartitionRows" /> 时自动创建微分区并刷盘
    /// </summary>
    /// <param name="tableName">目标表名</param>
    /// <param name="rows">行数据集合，每行数组长度必须与表模式列数一致</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>插入任务</returns>
    public async Task InsertBatchAsync(string tableName, IReadOnlyList<DataValue[]> rows,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_tables.TryGetValue(tableName, out var definition)) throw new AthenaException($"表 '{tableName}' 不存在");

        var schema = definition.Schema;
        var columnCount = schema.Count;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Length != columnCount)
                throw new AthenaException($"行数据列数 ({row.Length}) 与表 '{tableName}' 模式列数 ({columnCount}) 不匹配");
        }

        await _writeLock.WaitAsync(ct);
        try
        {
            var pending = _pendingRows[tableName];
            pending.AddRange(rows);

            while (pending.Count >= _options.MicroPartitionRows)
            {
                var batch = pending.GetRange(0, _options.MicroPartitionRows);
                pending.RemoveRange(0, _options.MicroPartitionRows);
                await FlushRowsAsync(tableName, definition, batch, ct);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    #endregion

    #region 索引管理

    /// <summary>
    ///     创建指定维度的向量索引，使用 <see cref="AthenaOptions.DefaultIndexType" /> 确定的索引类型
    /// </summary>
    /// <param name="name">索引名称</param>
    /// <param name="dimension">向量维度</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>创建任务</returns>
    public Task CreateVectorIndexAsync(string name, int dimension, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_vectorIndexes.ContainsKey(name)) throw new AthenaException($"向量索引 '{name}' 已存在");

        var indexType = _options.DefaultIndexType.ToLowerInvariant();
        IVectorIndex index = indexType switch
        {
            "hnsw" => new HnswIndex(dimension),
            _ => throw new AthenaException($"不支持的向量索引类型 '{_options.DefaultIndexType}'")
        };

        _vectorIndexes[name] = index;
        return Task.CompletedTask;
    }

    /// <summary>
    ///     创建图索引
    /// </summary>
    /// <param name="name">索引名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>创建任务</returns>
    public Task CreateGraphIndexAsync(string name, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_graphIndexes.ContainsKey(name)) throw new AthenaException($"图索引 '{name}' 已存在");

        _graphIndexes[name] = new CsrNeighborIndex();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     创建倒排索引
    /// </summary>
    /// <param name="name">索引名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>创建任务</returns>
    public Task CreateInvertedIndexAsync(string name, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (_invertedIndexes.ContainsKey(name)) throw new AthenaException($"倒排索引 '{name}' 已存在");

        _invertedIndexes[name] = new RoaringBitmapIndex();
        return Task.CompletedTask;
    }

    #endregion

    #region UDF 注册

    /// <summary>
    ///     注册标量 UDF
    /// </summary>
    /// <param name="udf">标量 UDF 实例</param>
    public void RegisterUdf(IScalarUdf udf)
    {
        _udfRegistry.Register(udf);
    }

    /// <summary>
    ///     注册聚合 UDF
    /// </summary>
    /// <param name="udf">聚合 UDF 实例</param>
    public void RegisterUdf(IAggregateUdf udf)
    {
        _udfRegistry.Register(udf);
    }

    #endregion
}

#endregion