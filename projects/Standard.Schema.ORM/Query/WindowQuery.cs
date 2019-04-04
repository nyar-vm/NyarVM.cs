namespace Hermes.YYDB.Query;

/// <summary>
///     窗口函数查询
/// </summary>
public sealed class WindowQuery : QueryExpression
{
    public WindowQuery(
        string targetTypeName,
        IReadOnlyList<WindowFunctionDefinition> windowFunctions,
        QueryPredicate? predicate = null,
        QueryOrdering? ordering = null,
        QueryPagination? pagination = null)
        : base(targetTypeName)
    {
        WindowFunctions = windowFunctions;
        Predicate = predicate;
        Ordering = ordering;
        Pagination = pagination;
    }

    public override string QueryKind => "window";

    /// <summary>
    ///     窗口函数列表
    /// </summary>
    public IReadOnlyList<WindowFunctionDefinition> WindowFunctions { get; }

    /// <summary>
    ///     查询条件
    /// </summary>
    public QueryPredicate? Predicate { get; }

    /// <summary>
    ///     排序规则
    /// </summary>
    public QueryOrdering? Ordering { get; }

    /// <summary>
    ///     分页
    /// </summary>
    public QueryPagination? Pagination { get; }
}

/// <summary>
///     窗口函数定义
/// </summary>
public sealed class WindowFunctionDefinition
{
    public WindowFunctionDefinition(
        WindowFunctionKind kind,
        string fieldName,
        string alias,
        IReadOnlyList<string>? partitionBy = null,
        IReadOnlyList<WindowOrderItem>? orderBy = null,
        WindowFrame? frame = null,
        int offset = 1,
        object? defaultValue = null)
    {
        Kind = kind;
        FieldName = fieldName;
        Alias = alias;
        PartitionBy = partitionBy ?? [];
        OrderBy = orderBy ?? [];
        Frame = frame;
        Offset = offset;
        DefaultValue = defaultValue;
    }

    /// <summary>
    ///     窗口函数类型
    /// </summary>
    public WindowFunctionKind Kind { get; }

    /// <summary>
    ///     函数参数字段名（如 LAG/LEAD 的目标字段）
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    ///     结果别名
    /// </summary>
    public string Alias { get; }

    /// <summary>
    ///     PARTITION BY 字段列表
    /// </summary>
    public IReadOnlyList<string> PartitionBy { get; }

    /// <summary>
    ///     ORDER BY 字段列表
    /// </summary>
    public IReadOnlyList<WindowOrderItem> OrderBy { get; }

    /// <summary>
    ///     窗口帧范围（ROWS BETWEEN ... AND ...）
    /// </summary>
    public WindowFrame? Frame { get; }

    /// <summary>
    ///     LAG/LEAD 偏移量
    /// </summary>
    public int Offset { get; }

    /// <summary>
    ///     LAG/LEAD 默认值
    /// </summary>
    public object? DefaultValue { get; }
}

/// <summary>
///     窗口函数类型
/// </summary>
public enum WindowFunctionKind
{
    RowNumber,
    Rank,
    DenseRank,
    PercentRank,
    NTile,
    Lag,
    Lead,
    FirstValue,
    LastValue,
    NthValue,
    Sum,
    Avg,
    Count,
    Min,
    Max
}

/// <summary>
///     窗口排序项
/// </summary>
public sealed class WindowOrderItem
{
    public WindowOrderItem(string fieldName, bool descending = false)
    {
        FieldName = fieldName;
        Descending = descending;
    }

    /// <summary>
    ///     字段名
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    ///     是否降序
    /// </summary>
    public bool Descending { get; }
}

/// <summary>
///     窗口帧范围
/// </summary>
public sealed class WindowFrame
{
    public WindowFrame(WindowFrameKind kind, WindowFrameBound start, WindowFrameBound end)
    {
        Kind = kind;
        Start = start;
        End = end;
    }

    /// <summary>
    ///     帧类型
    /// </summary>
    public WindowFrameKind Kind { get; }

    /// <summary>
    ///     起始位置
    /// </summary>
    public WindowFrameBound Start { get; }

    /// <summary>
    ///     结束位置
    /// </summary>
    public WindowFrameBound End { get; }
}

/// <summary>
///     帧类型
/// </summary>
public enum WindowFrameKind
{
    Rows,
    Range,
    Groups
}

/// <summary>
///     帧边界
/// </summary>
public sealed class WindowFrameBound
{
    public WindowFrameBound(WindowFrameBoundKind kind, int offsetRows = 0)
    {
        Kind = kind;
        OffsetRows = offsetRows;
    }

    /// <summary>
    ///     边界类型
    /// </summary>
    public WindowFrameBoundKind Kind { get; }

    /// <summary>
    ///     偏移行数（仅 OffsetPreceding/OffsetFollowing 有效）
    /// </summary>
    public int OffsetRows { get; }

    /// <summary>
    ///     UNBOUNDED PRECEDING
    /// </summary>
    public static WindowFrameBound UnboundedPreceding => new(WindowFrameBoundKind.UnboundedPreceding);

    /// <summary>
    ///     UNBOUNDED FOLLOWING
    /// </summary>
    public static WindowFrameBound UnboundedFollowing => new(WindowFrameBoundKind.UnboundedFollowing);

    /// <summary>
    ///     CURRENT ROW
    /// </summary>
    public static WindowFrameBound CurrentRow => new(WindowFrameBoundKind.CurrentRow);

    /// <summary>
    ///     创建指定偏移的 PRECEDING 边界
    /// </summary>
    public static WindowFrameBound Preceding(int offset)
    {
        return new WindowFrameBound(WindowFrameBoundKind.OffsetPreceding, offset);
    }

    /// <summary>
    ///     创建指定偏移的 FOLLOWING 边界
    /// </summary>
    public static WindowFrameBound Following(int offset)
    {
        return new WindowFrameBound(WindowFrameBoundKind.OffsetFollowing, offset);
    }
}

/// <summary>
///     帧边界类型
/// </summary>
public enum WindowFrameBoundKind
{
    UnboundedPreceding,
    UnboundedFollowing,
    CurrentRow,
    OffsetPreceding,
    OffsetFollowing
}