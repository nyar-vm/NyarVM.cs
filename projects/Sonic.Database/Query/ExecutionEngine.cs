using Olympus.Athena.Cache;
using Olympus.Athena.Core;
using Olympus.Athena.Storage;
using Std.Data.Text.Sql;

namespace Olympus.Athena.Query;

#region ExecutionEngine 查询执行引擎

/// <summary>
///     向量化查询执行引擎，构建算子管线并执行优化后的查询计划
/// </summary>
public sealed class ExecutionEngine
{
    #region 字段

    private readonly CacheManager _cacheManager;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建查询执行引擎
    /// </summary>
    /// <param name="cacheManager">缓存管理器，用于获取微分区数据</param>
    public ExecutionEngine(CacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     异步执行优化后的查询计划，流式返回结果行集
    /// </summary>
    /// <param name="plan">优化后的查询计划</param>
    /// <param name="partitions">分区数据源，分区 ID 到微分区的映射</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>结果行的异步可枚举序列</returns>
    public async Task<IAsyncEnumerable<DataValue[]>> ExecuteAsync(OptimizedQuery plan,
        IReadOnlyDictionary<UUIDv7, MicroPartition> partitions, CancellationToken ct = default)
    {
        var allRows = new List<DataValue[]>();

        await Task.Run(() =>
        {
            foreach (var partitionId in plan.RelevantPartitions)
            {
                ct.ThrowIfCancellationRequested();

                if (!partitions.TryGetValue(partitionId, out var partition)) continue;

                var partitionRows = ExecutePartition(plan, partition);
                allRows.AddRange(partitionRows);
            }
        }, ct);

        return allRows.ToAsyncEnumerable();
    }

    #endregion

    #region 分区执行

    private List<DataValue[]> ExecutePartition(OptimizedQuery plan, MicroPartition partition)
    {
        var query = plan.Query;
        var fromTable = query.FromTable;

        if (fromTable == null) return [];

        var schema = fromTable.Schema;
        var columnIndexMap = BuildColumnIndexMap(schema);
        var rowCount = partition.RowCount;

        var columnData = new Dictionary<int, object>();
        for (var i = 0; i < schema.Count; i++)
        {
            var colDesc = schema[i];
            switch (colDesc.DataType)
            {
                case DataType.Int64:
                    columnData[i] = partition.ReadColumn<long>(i);
                    break;
                case DataType.Float64:
                    columnData[i] = partition.ReadColumn<double>(i);
                    break;
                case DataType.Bool:
                    columnData[i] = partition.ReadColumn<bool>(i);
                    break;
                case DataType.Guid:
                    columnData[i] = partition.ReadColumn<Guid>(i);
                    break;
            }
        }

        var mask = ApplyFilter(query, columnData, columnIndexMap, rowCount);

        var projectedRows = ApplyProject(query, columnData, columnIndexMap, rowCount, mask);
        var projectedColumnNames = GetProjectedColumnNames(query);

        var afterAggregation = ApplyAggregation(query, projectedRows, projectedColumnNames, columnIndexMap);

        var afterJoin = ApplyHashJoin(query, afterAggregation);

        var finalRows = ApplyTopK(query, afterJoin, projectedColumnNames, columnIndexMap);

        return finalRows;
    }

    #endregion

    #region 列索引映射

    private static Dictionary<string, int> BuildColumnIndexMap(Schema schema)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < schema.Count; i++) map[schema[i].Name] = i;

        return map;
    }

    #endregion

    #region HashJoin

    private static List<DataValue[]> ApplyHashJoin(BoundQuery query, List<DataValue[]> rows)
    {
        if (query.Original.Joins.Count == 0) return rows;

        return rows;
    }

    #endregion

    #region TopK

    private static List<DataValue[]> ApplyTopK(BoundQuery query, List<DataValue[]> rows,
        List<string> projectedColumnNames, Dictionary<string, int> columnIndexMap)
    {
        if (query.Original.OrderBy.Count == 0 && query.Original.Limit == null) return rows;

        var k = query.Original.Limit ?? rows.Count;

        if (rows.Count <= k && query.Original.OrderBy.Count == 0) return [.. rows.Take(k)];

        if (query.Original.OrderBy.Count == 0) return [.. rows.Take(k)];

        var sortCol = query.Original.OrderBy[0];
        var sortColumnIndex = 0;

        if (sortCol.expression is BoundColumnRef bcr)
        {
            sortColumnIndex = projectedColumnNames.IndexOf(bcr.Name);
            if (sortColumnIndex < 0) sortColumnIndex = 0;
        }
        else if (sortCol.expression is ColumnRef cr)
        {
            sortColumnIndex = projectedColumnNames.IndexOf(cr.name);
            if (sortColumnIndex < 0) sortColumnIndex = 0;
        }

        var ascending = !sortCol.descending;

        rows.Sort((a, b) =>
        {
            if (sortColumnIndex >= a.Length || sortColumnIndex >= b.Length) return 0;

            var cmp = CompareDataValues(a[sortColumnIndex], b[sortColumnIndex]);
            return ascending ? cmp : -cmp;
        });

        return [.. rows.Take(k)];
    }

    #endregion

    #region 过滤

    private static bool[] ApplyFilter(BoundQuery query, Dictionary<int, object> columnData,
        Dictionary<string, int> columnIndexMap, int rowCount)
    {
        var mask = new bool[rowCount];
        Array.Fill(mask, true);

        if (query.Original.Where == null) return mask;

        for (var row = 0; row < rowCount; row++)
        {
            var rowValues = ReadRow(row, columnData);
            mask[row] = EvaluateExpr(query.Original.Where, rowValues, columnIndexMap);
        }

        return mask;
    }

    private static DataValue[] ReadRow(int rowIndex, Dictionary<int, object> columnData)
    {
        var row = new DataValue[columnData.Count];
        foreach (var (colIdx, chunk) in columnData) row[colIdx] = ReadCell(chunk, rowIndex);

        return row;
    }

    private static DataValue ReadCell(object chunk, int index)
    {
        var type = chunk.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ColumnChunk<>))
        {
            var elementType = type.GetGenericArguments()[0];

            if (elementType == typeof(long))
            {
                var values = ((ColumnChunk<long>)chunk).GetValues();
                return index < values.Length ? (DataValue)values[index] : default;
            }

            if (elementType == typeof(double))
            {
                var values = ((ColumnChunk<double>)chunk).GetValues();
                return index < values.Length ? (DataValue)values[index] : default;
            }

            if (elementType == typeof(bool))
            {
                var values = ((ColumnChunk<bool>)chunk).GetValues();
                return index < values.Length ? (DataValue)values[index] : default;
            }

            if (elementType == typeof(Guid))
            {
                var values = ((ColumnChunk<Guid>)chunk).GetValues();
                return index < values.Length ? (DataValue)values[index] : default;
            }
        }

        return default;
    }

    private static bool EvaluateExpr(SqlExpression expr, DataValue[] row, Dictionary<string, int> columnIndexMap)
    {
        switch (expr)
        {
            case BoundColumnRef bcr:
            {
                if (columnIndexMap.TryGetValue(bcr.Name, out var idx) && idx < row.Length)
                {
                    var val = row[idx];
                    return val.TryGetBool(out var b) && b;
                }

                return false;
            }
            case BinaryExpr binExpr:
            {
                var leftVal = EvalExprValue(binExpr.left, row, columnIndexMap);
                var rightVal = EvalExprValue(binExpr.right, row, columnIndexMap);

                switch (binExpr.@operator)
                {
                    case "=":
                        return leftVal.Equals(rightVal);
                    case "<>":
                        return !leftVal.Equals(rightVal);
                    case "<":
                        return CompareDataValues(leftVal, rightVal) < 0;
                    case ">":
                        return CompareDataValues(leftVal, rightVal) > 0;
                    case "<=":
                        return CompareDataValues(leftVal, rightVal) <= 0;
                    case ">=":
                        return CompareDataValues(leftVal, rightVal) >= 0;
                    case "AND":
                        return EvaluateExpr(binExpr.left, row, columnIndexMap)
                               && EvaluateExpr(binExpr.right, row, columnIndexMap);
                    case "OR":
                        return EvaluateExpr(binExpr.left, row, columnIndexMap)
                               || EvaluateExpr(binExpr.right, row, columnIndexMap);
                    default:
                        return false;
                }
            }
            default:
                return false;
        }
    }

    private static DataValue EvalExprValue(SqlExpression expr, DataValue[] row, Dictionary<string, int> columnIndexMap)
    {
        switch (expr)
        {
            case BoundColumnRef bcr:
            {
                if (columnIndexMap.TryGetValue(bcr.Name, out var idx) && idx < row.Length) return row[idx];

                return default;
            }
            case ColumnRef colRef:
            {
                if (columnIndexMap.TryGetValue(colRef.name, out var idx) && idx < row.Length) return row[idx];

                return default;
            }
            case LiteralValue lit:
                return ConvertLiteralToDataValue(lit);
            case FunctionCall funcCall:
            {
                if (funcCall.arguments.Count >= 2)
                {
                    var arg0 = EvalExprValue(funcCall.arguments[0], row, columnIndexMap);
                    var arg1 = EvalExprValue(funcCall.arguments[1], row, columnIndexMap);

                    if (string.Equals(funcCall.name, "distance", StringComparison.OrdinalIgnoreCase))
                        return ComputeDistance(arg0, arg1);

                    if (string.Equals(funcCall.name, "contains", StringComparison.OrdinalIgnoreCase))
                        return ComputeContains(arg0, arg1);
                }

                return default;
            }
            case BinaryExpr binExpr:
            {
                var left = EvalExprValue(binExpr.left, row, columnIndexMap);
                var right = EvalExprValue(binExpr.right, row, columnIndexMap);

                if (left.TryGetFloat64(out var lf) && right.TryGetFloat64(out var rf))
                    return binExpr.@operator switch
                    {
                        "<" => lf < rf,
                        ">" => lf > rf,
                        "<=" => lf <= rf,
                        ">=" => lf >= rf,
                        "=" => lf == rf,
                        "<>" => lf != rf,
                        _ => default(DataValue)
                    };

                return default;
            }
            default:
                return default;
        }
    }

    private static DataValue ConvertLiteralToDataValue(LiteralValue lit)
    {
        if (lit.value is null) return default;

        if (lit.value is long longVal) return longVal;

        if (lit.value is double doubleVal) return doubleVal;

        if (lit.value is string strVal)
        {
            if (long.TryParse(strVal, out var parsedLong)) return parsedLong;

            if (double.TryParse(strVal, out var parsedDouble)) return parsedDouble;

            return strVal;
        }

        return default;
    }

    private static DataValue ComputeDistance(DataValue a, DataValue b)
    {
        if (a.TryGetFloat64(out var af) && b.TryGetFloat64(out var bf)) return Math.Abs(af - bf);

        return 0.0;
    }

    private static DataValue ComputeContains(DataValue container, DataValue value)
    {
        if (container.TryGetString(out var str) && value.TryGetString(out var sub)
                                                && str is not null && sub is not null)
            return str.Contains(sub, StringComparison.OrdinalIgnoreCase);

        return false;
    }

    private static int CompareDataValues(DataValue a, DataValue b)
    {
        if (a.TryGetInt64(out var aInt) && b.TryGetInt64(out var bInt)) return aInt.CompareTo(bInt);

        if (a.TryGetFloat64(out var aFloat) && b.TryGetFloat64(out var bFloat)) return aFloat.CompareTo(bFloat);

        if (a.TryGetString(out var aStr) && b.TryGetString(out var bStr))
            return string.Compare(aStr, bStr, StringComparison.Ordinal);

        return 0;
    }

    #endregion

    #region 投影

    private static List<DataValue[]> ApplyProject(BoundQuery query, Dictionary<int, object> columnData,
        Dictionary<string, int> columnIndexMap, int rowCount, bool[] mask)
    {
        var projectedColumnNames = GetProjectedColumnNames(query);
        var result = new List<DataValue[]>();

        for (var row = 0; row < rowCount; row++)
        {
            if (!mask[row]) continue;

            var projectedRow = new DataValue[projectedColumnNames.Count];
            for (var col = 0; col < projectedColumnNames.Count; col++)
            {
                var colName = projectedColumnNames[col];
                if (columnIndexMap.TryGetValue(colName, out var colIdx) &&
                    columnData.TryGetValue(colIdx, out var chunk)) projectedRow[col] = ReadCell(chunk, row);
            }

            result.Add(projectedRow);
        }

        return result;
    }

    private static List<string> GetProjectedColumnNames(BoundQuery query)
    {
        var names = new List<string>();

        foreach (var col in query.Original.Columns)
            if (col.expression is BoundColumnRef bcr)
                names.Add(bcr.Name);
            else if (col.expression is ColumnRef cr) names.Add(cr.name);

        return names;
    }

    #endregion

    #region 聚合

    private static List<DataValue[]> ApplyAggregation(BoundQuery query, List<DataValue[]> rows,
        List<string> projectedColumnNames, Dictionary<string, int> columnIndexMap)
    {
        if (query.Original.GroupBy.Count == 0 && !HasAggregation(query)) return rows;

        var groupByIndex = -1;
        if (query.Original.GroupBy.Count > 0)
        {
            var groupCol = query.Original.GroupBy[0];
            if (groupCol is BoundColumnRef bcr && columnIndexMap.TryGetValue(bcr.Name, out var idx))
                groupByIndex = projectedColumnNames.IndexOf(bcr.Name);

            if (groupCol is ColumnRef cr && columnIndexMap.TryGetValue(cr.name, out var idx2))
                groupByIndex = projectedColumnNames.IndexOf(cr.name);
        }

        var groups = new Dictionary<DataValue, List<DataValue[]>>();

        foreach (var row in rows)
        {
            var key = groupByIndex >= 0 && groupByIndex < row.Length ? row[groupByIndex] : default;

            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }

            list.Add(row);
        }

        var result = new List<DataValue[]>();

        foreach (var (key, groupRows) in groups)
        {
            var aggRow = new DataValue[projectedColumnNames.Count + 1];
            aggRow[0] = groupRows.Count;
            aggRow[1] = key;
            result.Add(aggRow);
        }

        return result;
    }

    private static bool HasAggregation(BoundQuery query)
    {
        foreach (var col in query.Original.Columns)
            if (col.expression is FunctionCall funcCall)
            {
                var name = funcCall.name.ToUpperInvariant();
                if (name is "COUNT" or "SUM" or "AVG" or "MIN" or "MAX") return true;
            }

        return false;
    }

    #endregion
}

#endregion