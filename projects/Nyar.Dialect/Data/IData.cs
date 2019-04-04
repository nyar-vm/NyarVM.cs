using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Data;

/// <summary>
///     Data 方言的 OA 接口定义。
///     该接口声明查询、聚合、排序与统计相关操作。
/// </summary>
[Dialect("data")]
public interface IData<T>
{
    /// <summary>
    ///     全表扫描。
    /// </summary>
    [Operator("scan")]
    Term<T> Scan(string tableName);

    /// <summary>
    ///     索引扫描。
    /// </summary>
    [Operator("index_scan")]
    Term<T> IndexScan(string tableName, string indexName, Term<T> predicate);

    /// <summary>
    ///     过滤数据流。
    /// </summary>
    [Operator("filter")]
    Term<T> Filter(Term<T> predicate, Term<T> data);

    /// <summary>
    ///     投影列集合。
    /// </summary>
    [Operator("project")]
    Term<T> Project(IReadOnlyList<string> columns, Term<T> data);

    /// <summary>
    ///     连接两个数据流。
    /// </summary>
    [Operator("join")]
    Term<T> Join(JoinType type, Term<T> left, Term<T> right, Term<T> condition);

    /// <summary>
    ///     聚合单列。
    /// </summary>
    [Operator("aggregate")]
    Term<T> Aggregate(AggType type, Term<T> column, Term<T> data);

    /// <summary>
    ///     分组聚合。
    /// </summary>
    [Operator("group_by")]
    Term<T> GroupBy(IReadOnlyList<string> keys, IReadOnlyList<AggregateExpr> aggregates, Term<T> data);

    /// <summary>
    ///     后过滤。
    /// </summary>
    [Operator("having")]
    Term<T> Having(Term<T> predicate, Term<T> data);

    /// <summary>
    ///     窗口函数。
    /// </summary>
    [Operator("window_function")]
    Term<T> WindowFunction(
        WindowFuncType type,
        Term<T> column,
        IReadOnlyList<string> partitionBy,
        WindowOrderBy? orderBy,
        WindowFrame? frame,
        Term<T> data);

    /// <summary>
    ///     去重。
    /// </summary>
    [Operator("distinct")]
    Term<T> Distinct(IReadOnlyList<string> columns, Term<T> data);

    /// <summary>
    ///     合并查询结果。
    /// </summary>
    [Operator("union")]
    Term<T> Union(UnionKind kind, Term<T> left, Term<T> right);

    /// <summary>
    ///     排序。
    /// </summary>
    [Operator("order_by")]
    Term<T> OrderBy(Term<T> key, bool ascending, Term<T> data);

    /// <summary>
    ///     限制返回条数。
    /// </summary>
    [Operator("limit")]
    Term<T> Limit(Term<T> count, Term<T> data);

    /// <summary>
    ///     表统计信息。
    /// </summary>
    [Operator("table_stats")]
    Term<T> TableStats(
        string tableName,
        long rowCount,
        IReadOnlyDictionary<string, ColumnStats> columns);
}