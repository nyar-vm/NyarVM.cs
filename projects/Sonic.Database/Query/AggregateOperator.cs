using Olympus.Athena.Core;

namespace Olympus.Athena.Query;

#region AggregateOperator 聚合算子

/// <summary>
///     聚合算子，支持 Count、Sum、Avg、Min、Max 聚合计算
/// </summary>
public sealed class AggregateOperator : VectorizedOperator
{
    #region 构造函数

    /// <summary>
    ///     创建聚合算子
    /// </summary>
    /// <param name="aggregateColumnIndex">聚合列的索引</param>
    /// <param name="aggregateType">聚合类型</param>
    /// <param name="groupByColumnIndex">可选的 GROUP BY 列索引</param>
    public AggregateOperator(int aggregateColumnIndex, AggregateType aggregateType, int? groupByColumnIndex = null)
    {
        _aggregateColumnIndex = aggregateColumnIndex;
        _aggregateType = aggregateType;
        _groupByColumnIndex = groupByColumnIndex;
        _inputRows = [];
        Results = [];
    }

    #endregion

    #region 属性

    /// <summary>
    ///     聚合结果列表，Key 为分组键，Result 为聚合值
    /// </summary>
    public List<(DataValue Key, DataValue Result)> Results { get; }

    #endregion

    #region 执行

    /// <summary>
    ///     对输入行集执行聚合计算
    /// </summary>
    public override void Execute()
    {
        CollectInputRows();

        var groups = new Dictionary<DataValue, List<DataValue>>();

        foreach (var row in _inputRows)
        {
            if (_aggregateColumnIndex >= row.Length) continue;

            var key = _groupByColumnIndex is not null && _groupByColumnIndex.Value < row.Length
                ? row[_groupByColumnIndex.Value]
                : default;

            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }

            list.Add(row[_aggregateColumnIndex]);
        }

        foreach (var (key, values) in groups)
        {
            var result = ComputeAggregate(values);
            Results.Add((key, result));
        }

        foreach (var child in Children) child.Execute();
    }

    #endregion

    #region 输入收集

    private void CollectInputRows()
    {
        foreach (var child in Children)
            switch (child)
            {
                case ProjectOperator projectOp:
                    _inputRows.AddRange(projectOp.Results);
                    break;
            }
    }

    #endregion

    #region 字段

    private readonly int _aggregateColumnIndex;
    private readonly AggregateType _aggregateType;
    private readonly int? _groupByColumnIndex;
    private readonly List<DataValue[]> _inputRows;

    #endregion

    #region 聚合计算

    private DataValue ComputeAggregate(List<DataValue> values)
    {
        switch (_aggregateType)
        {
            case AggregateType.Count:
                return values.Count;
            case AggregateType.Sum:
                return ComputeSum(values);
            case AggregateType.Avg:
                return ComputeAvg(values);
            case AggregateType.Min:
                return ComputeMin(values);
            case AggregateType.Max:
                return ComputeMax(values);
            default:
                return default;
        }
    }

    private static DataValue ComputeSum(List<DataValue> values)
    {
        if (values.Count == 0) return 0L;

        var sum = 0.0;
        var hasDouble = false;

        foreach (var v in values)
            if (v.TryGetInt64(out var iVal))
            {
                sum += iVal;
            }
            else if (v.TryGetFloat64(out var fVal))
            {
                sum += fVal;
                hasDouble = true;
            }

        return hasDouble ? (DataValue)sum : (long)sum;
    }

    private static DataValue ComputeAvg(List<DataValue> values)
    {
        if (values.Count == 0) return 0.0;

        var sum = ComputeSum(values);
        double total;

        if (sum.TryGetInt64(out var iVal))
            total = iVal;
        else if (sum.TryGetFloat64(out var fVal))
            total = fVal;
        else
            return 0.0;

        return total / values.Count;
    }

    private static DataValue ComputeMin(List<DataValue> values)
    {
        if (values.Count == 0) return default;

        var min = values[0];

        for (var i = 1; i < values.Count; i++)
            if (CompareValues(values[i], min) < 0)
                min = values[i];

        return min;
    }

    private static DataValue ComputeMax(List<DataValue> values)
    {
        if (values.Count == 0) return default;

        var max = values[0];

        for (var i = 1; i < values.Count; i++)
            if (CompareValues(values[i], max) > 0)
                max = values[i];

        return max;
    }

    private static int CompareValues(DataValue a, DataValue b)
    {
        if (a.TryGetInt64(out var aInt) && b.TryGetInt64(out var bInt)) return aInt.CompareTo(bInt);

        if (a.TryGetFloat64(out var aFloat) && b.TryGetFloat64(out var bFloat)) return aFloat.CompareTo(bFloat);

        if (a.TryGetString(out var aStr) && b.TryGetString(out var bStr))
            return string.Compare(aStr, bStr, StringComparison.Ordinal);

        return 0;
    }

    #endregion
}

#region AggregateType 聚合类型

/// <summary>
///     聚合函数的类型
/// </summary>
public enum AggregateType
{
    Count,
    Sum,
    Avg,
    Min,
    Max
}

#endregion

#endregion