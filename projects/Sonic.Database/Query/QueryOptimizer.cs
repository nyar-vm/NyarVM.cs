using Olympus.Athena.Core;
using Olympus.Athena.Storage;
using Std.Data.Text.Sql;

namespace Olympus.Athena.Query;

#region OptimizedQuery 优化后的查询

/// <summary>
///     优化后的查询，包含列裁剪、分区裁剪和索引建议的结果
/// </summary>
public sealed class OptimizedQuery
{
    /// <summary>
    ///     创建优化后的查询
    /// </summary>
    /// <param name="requiredColumns">必需列名列表</param>
    /// <param name="relevantPartitions">相关分区列表</param>
    /// <param name="vectorIndexHint">向量索引建议</param>
    /// <param name="graphIndexHint">图索引建议</param>
    /// <param name="query">原始绑定查询</param>
    public OptimizedQuery(IReadOnlyList<string> requiredColumns, IReadOnlyList<UUIDv7> relevantPartitions,
        string? vectorIndexHint, string? graphIndexHint, BoundQuery query)
    {
        RequiredColumns = requiredColumns;
        RelevantPartitions = relevantPartitions;
        VectorIndexHint = vectorIndexHint;
        GraphIndexHint = graphIndexHint;
        Query = query;
    }

    /// <summary>
    ///     列裁剪后的必需列名列表
    /// </summary>
    public IReadOnlyList<string> RequiredColumns { get; }

    /// <summary>
    ///     分区裁剪后的相关分区 ID 列表
    /// </summary>
    public IReadOnlyList<UUIDv7> RelevantPartitions { get; }

    /// <summary>
    ///     向量索引建议，如果 WHERE 中有 distance() 调用
    /// </summary>
    public string? VectorIndexHint { get; }

    /// <summary>
    ///     图索引建议，如果有 MATCH 子句
    /// </summary>
    public string? GraphIndexHint { get; }

    /// <summary>
    ///     原始绑定查询
    /// </summary>
    public BoundQuery Query { get; }
}

#endregion

#region QueryOptimizer 查询优化器

/// <summary>
///     基于规则的查询优化器，执行列裁剪、分区裁剪和索引选择
/// </summary>
public sealed class QueryOptimizer
{
    #region 字段

    private readonly IReadOnlyDictionary<UUIDv7, PartitionMetadata> _partitions;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建查询优化器
    /// </summary>
    /// <param name="partitions">分区 ID 到分区元数据的映射</param>
    public QueryOptimizer(IReadOnlyDictionary<UUIDv7, PartitionMetadata> partitions)
    {
        _partitions = partitions;
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     优化绑定查询，应用列裁剪、分区裁剪和索引选择规则
    /// </summary>
    /// <param name="bound">绑定后的查询</param>
    /// <returns>优化后的查询</returns>
    public OptimizedQuery Optimize(BoundQuery bound)
    {
        var requiredColumns = PruneColumns(bound);
        var relevantPartitions = PrunePartitions(bound);
        var vectorIndexHint = DetectVectorIndex(bound);
        var graphIndexHint = DetectGraphIndex(bound);

        return new OptimizedQuery(requiredColumns, relevantPartitions, vectorIndexHint, graphIndexHint, bound);
    }

    #endregion

    #region 列裁剪

    private static IReadOnlyList<string> PruneColumns(BoundQuery bound)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in bound.Original.Columns) CollectColumns(col.expression, columns);

        if (bound.Original.Where != null) CollectColumns(bound.Original.Where, columns);

        foreach (var gb in bound.Original.GroupBy) CollectColumns(gb, columns);

        foreach (var ob in bound.Original.OrderBy) CollectColumns(ob.expression, columns);

        foreach (var join in bound.Original.Joins)
            if (join.on_condition != null)
                CollectColumns(join.on_condition, columns);

        return columns.ToList().AsReadOnly();
    }

    private static void CollectColumns(SqlExpression expr, HashSet<string> columns)
    {
        switch (expr)
        {
            case BoundColumnRef bcr:
                columns.Add(bcr.Name);
                break;
            case ColumnRef cr:
                columns.Add(cr.name);
                break;
            case BinaryExpr binExpr:
                CollectColumns(binExpr.left, columns);
                CollectColumns(binExpr.right, columns);
                break;
            case FunctionCall funcCall:
            {
                foreach (var arg in funcCall.arguments) CollectColumns(arg, columns);

                break;
            }
        }
    }

    #endregion

    #region 分区裁剪

    private IReadOnlyList<UUIDv7> PrunePartitions(BoundQuery bound)
    {
        if (bound.Original.Where == null) return _partitions.Keys.ToList().AsReadOnly();

        var relevantIds = new List<UUIDv7>();

        foreach (var (partitionId, metadata) in _partitions)
            if (EvaluatePartition(bound, metadata))
                relevantIds.Add(partitionId);

        return relevantIds.AsReadOnly();
    }

    private static bool EvaluatePartition(BoundQuery bound, PartitionMetadata metadata)
    {
        if (bound.Original.Where == null) return true;

        return EvaluateExprWithZoneMap(bound.Original.Where, metadata);
    }

    private static bool EvaluateExprWithZoneMap(SqlExpression expr, PartitionMetadata metadata)
    {
        switch (expr)
        {
            case BinaryExpr { @operator: "AND" } binExpr:
                return EvaluateExprWithZoneMap(binExpr.left, metadata)
                       && EvaluateExprWithZoneMap(binExpr.right, metadata);
            case BinaryExpr { @operator: "OR" } binExpr:
                return EvaluateExprWithZoneMap(binExpr.left, metadata)
                       || EvaluateExprWithZoneMap(binExpr.right, metadata);
            case BinaryExpr binExpr:
            {
                var colRef = ExtractColumnRef(binExpr.left) ?? ExtractColumnRef(binExpr.right);
                var lit = ExtractLiteral(binExpr.left) ?? ExtractLiteral(binExpr.right);

                if (colRef is BoundColumnRef bcr && lit != null)
                {
                    if (!metadata.ColumnMaps.TryGetValue(bcr.ColumnIndex, out var zoneMap)) return true;

                    var litValue = GetInt64Value(lit);

                    switch (binExpr.@operator)
                    {
                        case "=":
                            return litValue >= zoneMap.Min && litValue <= zoneMap.Max;
                        case "<":
                            return zoneMap.Min < litValue;
                        case ">":
                            return zoneMap.Max > litValue;
                        case "<=":
                            return zoneMap.Min <= litValue;
                        case ">=":
                            return zoneMap.Max >= litValue;
                        case "<>":
                            return true;
                    }
                }

                return true;
            }
            default:
                return true;
        }
    }

    private static BoundColumnRef? ExtractColumnRef(SqlExpression expr)
    {
        return expr switch
        {
            BoundColumnRef bcr => bcr,
            ColumnRef cr => new BoundColumnRef(cr.name),
            _ => null
        };
    }

    private static LiteralValue? ExtractLiteral(SqlExpression expr)
    {
        return expr as LiteralValue;
    }

    private static long GetInt64Value(LiteralValue lit)
    {
        if (lit.value is long longVal) return longVal;

        if (lit.value is double doubleVal) return (long)doubleVal;

        if (lit.value is string strVal)
        {
            if (long.TryParse(strVal, out var parsedLong)) return parsedLong;

            if (double.TryParse(strVal, out var parsedDouble)) return (long)parsedDouble;
        }

        return 0;
    }

    #endregion

    #region 索引检测

    private static string? DetectVectorIndex(BoundQuery bound)
    {
        if (bound.Original.Where != null && HasDistanceCall(bound.Original.Where)) return "建议使用向量索引优化 distance() 查询";

        return null;
    }

    private static string? DetectGraphIndex(BoundQuery bound)
    {
        if (bound.Original.Joins.Count > 0)
            foreach (var join in bound.Original.Joins)
                if (join.on_condition != null && HasMatchCall(join.on_condition))
                    return $"建议使用图索引优化 {join.name} 连接";

        if (bound.Original.Where != null && HasMatchCall(bound.Original.Where)) return "建议使用图索引优化 match() 查询";

        return null;
    }

    private static bool HasDistanceCall(SqlExpression expr)
    {
        switch (expr)
        {
            case FunctionCall funcCall
                when string.Equals(funcCall.name, "distance", StringComparison.OrdinalIgnoreCase):
                return true;
            case BinaryExpr binExpr:
                return HasDistanceCall(binExpr.left) || HasDistanceCall(binExpr.right);
            default:
                return false;
        }
    }

    private static bool HasMatchCall(SqlExpression expr)
    {
        switch (expr)
        {
            case FunctionCall funcCall when string.Equals(funcCall.name, "match", StringComparison.OrdinalIgnoreCase):
                return true;
            case BinaryExpr binExpr:
                return HasMatchCall(binExpr.left) || HasMatchCall(binExpr.right);
            default:
                return false;
        }
    }

    #endregion
}

#endregion