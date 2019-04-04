using Olympus.Athena.Cache;
using Olympus.Athena.Core;
using Olympus.Athena.Storage;
using Std.Data.Text.Sql;

namespace Olympus.Athena.Query;

#region QueryEngine 查询引擎

/// <summary>
///     Athena SQL 查询引擎，负责解析 SQL 并执行向量化查询的完整管线
/// </summary>
public sealed class QueryEngine
{
    #region 构造函数

    /// <summary>
    ///     创建查询引擎实例
    /// </summary>
    /// <param name="tables">已注册的表定义字典</param>
    /// <param name="cache">缓存管理器</param>
    /// <param name="reader">微分区读取器</param>
    public QueryEngine(IReadOnlyDictionary<string, TableDefinition> tables, CacheManager cache,
        MicroPartitionReader reader)
    {
        _tables = tables;
        _cache = cache;
        _reader = reader;
    }

    #endregion

    #region ExecuteAsync

    /// <summary>
    ///     执行 SQL 查询，以异步流方式返回结果行
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>查询结果的异步可枚举流</returns>
    public async IAsyncEnumerable<DataValue[]> ExecuteAsync(string sql,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var parser = new SqlParser();
        var parseResult = parser.parse(sql);

        if (!parseResult.IsSuccess || parseResult.value is not SelectStatement)
            throw new AthenaException("SQL 解析失败或不是 SELECT 语句");

        var binder = new QueryBinder(_tables);
        var bound = binder.Bind(parseResult.value);

        var intentBuilder = new SqlIntentBuilder();
        var intent = intentBuilder.Build(bound);

        var partitionMetadatas = await LoadPartitionMetadatasAsync(bound.FromTable, ct);

        var optimizer = new QueryOptimizer(partitionMetadatas);
        var optimized = optimizer.Optimize(bound);

        var schema = bound.FromTable?.Schema ?? new Schema();
        var partitions = await LoadRelevantPartitionsAsync(optimized.RelevantPartitions, schema, ct);

        var executionEngine = new ExecutionEngine(_cache);
        var resultStream = await executionEngine.ExecuteAsync(optimized, partitions, ct);

        await foreach (var row in resultStream.WithCancellation(ct)) yield return row;
    }

    #endregion

    #region 字段

    private readonly IReadOnlyDictionary<string, TableDefinition> _tables;
    private readonly CacheManager _cache;
    private readonly MicroPartitionReader _reader;

    #endregion

    #region Private Helpers

    private async Task<IReadOnlyDictionary<UUIDv7, PartitionMetadata>> LoadPartitionMetadatasAsync(
        TableDefinition? fromTable, CancellationToken ct)
    {
        var result = new Dictionary<UUIDv7, PartitionMetadata>();

        if (fromTable == null) return result;

        var partitionIds = await _reader.ListPartitionsAsync(fromTable.Name, ct);
        foreach (var id in partitionIds)
        {
            var metadata = await _reader.ReadMetadataAsync(id, ct);
            if (metadata != null) result[id] = metadata;
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<UUIDv7, MicroPartition>> LoadRelevantPartitionsAsync(
        IReadOnlyList<UUIDv7> partitionIds, Schema schema, CancellationToken ct)
    {
        var result = new Dictionary<UUIDv7, MicroPartition>();

        foreach (var id in partitionIds)
        {
            var partition = await _reader.ReadPartitionAsync(id, schema, ct);
            if (partition != null) result[id] = partition;
        }

        return result;
    }

    #endregion
}

#endregion