using Nyar.IR.Intent;

namespace Nyar.Dialect.Data;

[AlgebraNode]
public sealed partial record Scan(string table_name) : AlgebraNode;

[AlgebraNode]
public sealed partial record IndexScan(string table_name, string index_name, Id predicate) : AlgebraNode;

[AlgebraNode]
public sealed partial record Filter(Id predicate, Id data) : AlgebraNode;

[AlgebraNode]
public sealed partial record Project(IReadOnlyList<string> columns, Id data) : AlgebraNode;

[AlgebraNode]
public sealed partial record Join(JoinType type, Id left, Id right, Id condition) : AlgebraNode;

/// <summary>
///     连接类型
/// </summary>
public enum JoinType
{
    /// <summary>
    ///     内连接
    /// </summary>
    inner,

    /// <summary>
    ///     左外连接
    /// </summary>
    left,

    /// <summary>
    ///     右外连接
    /// </summary>
    right,

    /// <summary>
    ///     全外连接
    /// </summary>
    full,

    /// <summary>
    ///     交叉连接
    /// </summary>
    cross,

    /// <summary>
    ///     半连接（IN/EXISTS 子查询），返回左表中在右表有匹配的行
    /// </summary>
    semi,

    /// <summary>
    ///     反半连接（NOT IN/NOT EXISTS 子查询），返回左表中在右表无匹配的行
    /// </summary>
    anti_semi
}

[AlgebraNode]
public sealed partial record Aggregate(AggType type, Id column, Id data) : AlgebraNode;

public enum AggType
{
    sum,
    count,
    avg,
    min,
    max
}

[AlgebraNode]
public sealed partial record GroupBy(IReadOnlyList<string> keys, IReadOnlyList<AggregateExpr> aggregates, Id data)
    : AlgebraNode;

/// <summary>
///     聚合表达式，描述 GROUP BY 中的聚合函数调用
/// </summary>
public sealed record AggregateExpr(AggType type, string column, string? alias);

[AlgebraNode]
public sealed partial record Having(Id predicate, Id data) : AlgebraNode;

[AlgebraNode]
public sealed partial record WindowFunction(
    WindowFuncType type,
    Id column,
    IReadOnlyList<string> partition_by,
    WindowOrderBy? order_by,
    WindowFrame? frame,
    Id data) : AlgebraNode;

/// <summary>
///     窗口函数类型
/// </summary>
public enum WindowFuncType
{
    row_number,
    rank,
    dense_rank,
    sum,
    avg,
    count,
    min,
    max,
    lag,
    lead,
    first_value,
    last_value,
    n_tile
}

/// <summary>
///     窗口排序定义
/// </summary>
public sealed record WindowOrderBy(string column, bool ascending);

/// <summary>
///     窗口帧定义（ROWS/RANGE BETWEEN ... AND ...）
/// </summary>
public sealed record WindowFrame(WindowFrameMode mode, WindowFrameBound start, WindowFrameBound end);

/// <summary>
///     窗口帧模式
/// </summary>
public enum WindowFrameMode
{
    rows,
    range,
    groups
}

/// <summary>
///     窗口帧边界
/// </summary>
public sealed record WindowFrameBound(WindowFrameBoundType type, long? offset);

/// <summary>
///     窗口帧边界类型
/// </summary>
public enum WindowFrameBoundType
{
    preceding,
    following,
    current_row,
    unbounded_preceding,
    unbounded_following
}

[AlgebraNode]
public sealed partial record Distinct(IReadOnlyList<string> columns, Id data) : AlgebraNode;

[AlgebraNode]
public sealed partial record Subquery(SubqueryKind kind, Id query, Id? outer_ref) : AlgebraNode;

/// <summary>
///     子查询类型
/// </summary>
public enum SubqueryKind
{
    scalar,
    exists,
    @in,
    not_exists,
    not_in
}

[AlgebraNode]
public sealed partial record Union(UnionKind kind, Id left, Id right) : AlgebraNode;

/// <summary>
///     合并类型
/// </summary>
public enum UnionKind
{
    all,
    distinct
}

[AlgebraNode]
public sealed partial record OrderBy(Id key, bool ascending, Id data) : AlgebraNode;

[AlgebraNode]
public sealed partial record Limit(Id count, Id data) : AlgebraNode;

[AlgebraNode]
public sealed partial record TableStats(
    string table_name,
    long row_count,
    IReadOnlyDictionary<string, ColumnStats> columns) : AlgebraNode;

public sealed record ColumnStats(long distinct_count, long null_count, object? min_value, object? max_value);