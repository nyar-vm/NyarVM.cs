using Nyar.IR.Intent;

namespace Nyar.Dialect.Data;

/// <summary>
///     SQL 聚合函数库，提供 TPC-H Q1-Q5 所需的全部聚合函数
/// </summary>
public static class AggregateFunctions
{
    /// <summary>
    ///     标准聚合函数名称映射
    /// </summary>
    public static readonly IReadOnlyDictionary<AggType, string> StandardAggNames = new Dictionary<AggType, string>
    {
        { AggType.sum, "SUM" },
        { AggType.count, "COUNT" },
        { AggType.avg, "AVG" },
        { AggType.min, "MIN" },
        { AggType.max, "MAX" }
    };

    /// <summary>
    ///     TPC-H 常用聚合表达式工厂
    /// </summary>
    public static AggregateExpr Sum(string column, string? alias = null)
    {
        return new AggregateExpr(AggType.sum, column, alias);
    }

    /// <summary>
    ///     创建 COUNT 聚合表达式
    /// </summary>
    public static AggregateExpr Count(string column, string? alias = null)
    {
        return new AggregateExpr(AggType.count, column, alias);
    }

    /// <summary>
    ///     创建 COUNT(*) 聚合表达式
    /// </summary>
    public static AggregateExpr CountAll(string? alias = null)
    {
        return new AggregateExpr(AggType.count, "*", alias);
    }

    /// <summary>
    ///     创建 AVG 聚合表达式
    /// </summary>
    public static AggregateExpr Avg(string column, string? alias = null)
    {
        return new AggregateExpr(AggType.avg, column, alias);
    }

    /// <summary>
    ///     创建 MIN 聚合表达式
    /// </summary>
    public static AggregateExpr Min(string column, string? alias = null)
    {
        return new AggregateExpr(AggType.min, column, alias);
    }

    /// <summary>
    ///     创建 MAX 聚合表达式
    /// </summary>
    public static AggregateExpr Max(string column, string? alias = null)
    {
        return new AggregateExpr(AggType.max, column, alias);
    }

    /// <summary>
    ///     将聚合表达式转换为 SQL 片段
    /// </summary>
    public static string ToSql(this AggregateExpr expr)
    {
        var funcName = StandardAggNames[expr.type];
        var sql = $"{funcName}({expr.column})";
        return expr.alias is not null ? $"{sql} AS {expr.alias}" : sql;
    }
}

/// <summary>
///     SQL 窗口函数库，提供 TPC-H 和常见分析查询所需的窗口函数
/// </summary>
public static class WindowFunctions
{
    /// <summary>
    ///     标准窗口函数名称映射
    /// </summary>
    public static readonly IReadOnlyDictionary<WindowFuncType, string> StandardWindowNames =
        new Dictionary<WindowFuncType, string>
        {
            { WindowFuncType.row_number, "ROW_NUMBER" },
            { WindowFuncType.rank, "RANK" },
            { WindowFuncType.dense_rank, "DENSE_RANK" },
            { WindowFuncType.sum, "SUM" },
            { WindowFuncType.avg, "AVG" },
            { WindowFuncType.count, "COUNT" },
            { WindowFuncType.min, "MIN" },
            { WindowFuncType.max, "MAX" },
            { WindowFuncType.lag, "LAG" },
            { WindowFuncType.lead, "LEAD" },
            { WindowFuncType.first_value, "FIRST_VALUE" },
            { WindowFuncType.last_value, "LAST_VALUE" },
            { WindowFuncType.n_tile, "NTILE" }
        };

    /// <summary>
    ///     创建 ROW_NUMBER() OVER(PARTITION BY ... ORDER BY ...) 窗口函数
    /// </summary>
    public static WindowFunction RowNumber(
        IReadOnlyList<string> partitionBy, WindowOrderBy orderBy, Id data)
    {
        return new WindowFunction(WindowFuncType.row_number, data, partitionBy, orderBy, null, data);
    }

    /// <summary>
    ///     创建 RANK() OVER(PARTITION BY ... ORDER BY ...) 窗口函数
    /// </summary>
    public static WindowFunction Rank(
        IReadOnlyList<string> partitionBy, WindowOrderBy orderBy, Id data)
    {
        return new WindowFunction(WindowFuncType.rank, data, partitionBy, orderBy, null, data);
    }

    /// <summary>
    ///     创建 SUM() OVER(PARTITION BY ... ORDER BY ...) 窗口函数
    /// </summary>
    public static WindowFunction RunningSum(
        Id column, IReadOnlyList<string> partitionBy, WindowOrderBy orderBy, Id data)
    {
        var frame = new WindowFrame(
            WindowFrameMode.rows,
            new WindowFrameBound(WindowFrameBoundType.unbounded_preceding, null),
            new WindowFrameBound(WindowFrameBoundType.current_row, null));
        return new WindowFunction(WindowFuncType.sum, column, partitionBy, orderBy, frame, data);
    }

    /// <summary>
    ///     创建 LAG(column, offset) 窗口函数
    /// </summary>
    public static WindowFunction Lag(
        Id column, IReadOnlyList<string> partitionBy, WindowOrderBy orderBy, Id data)
    {
        return new WindowFunction(WindowFuncType.lag, column, partitionBy, orderBy, null, data);
    }

    /// <summary>
    ///     将窗口帧边界转换为 SQL 片段
    /// </summary>
    public static string ToSql(this WindowFrameBound bound)
    {
        return bound.type switch
        {
            WindowFrameBoundType.current_row => "CURRENT ROW",
            WindowFrameBoundType.unbounded_preceding => "UNBOUNDED PRECEDING",
            WindowFrameBoundType.unbounded_following => "UNBOUNDED FOLLOWING",
            WindowFrameBoundType.preceding => $"{bound.offset} PRECEDING",
            WindowFrameBoundType.following => $"{bound.offset} FOLLOWING",
            _ => "CURRENT ROW"
        };
    }

    /// <summary>
    ///     将窗口帧转换为 SQL 片段
    /// </summary>
    public static string ToSql(this WindowFrame frame)
    {
        var mode = frame.mode switch
        {
            WindowFrameMode.rows => "ROWS",
            WindowFrameMode.range => "RANGE",
            WindowFrameMode.groups => "GROUPS",
            _ => "ROWS"
        };
        return $"{mode} BETWEEN {frame.start.ToSql()} AND {frame.end.ToSql()}";
    }
}