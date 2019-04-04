using Nyar.ObjectAlgebra;
using Std.Data.Text.Sql;

namespace Nyar.Dialect.Query;

/// <summary>
///     Query 方言的 OA 接口定义。
///     该接口声明查询计划中的扫描、投影、过滤、连接、聚合与排序操作。
/// </summary>
[Dialect("query")]
public interface IQuery<T>
{
    /// <summary>
    ///     全表扫描。
    /// </summary>
    [Operator("table_scan")]
    Term<T> table_scan(string tableName, string? alias);

    /// <summary>
    ///     索引扫描。
    /// </summary>
    [Operator("index_scan")]
    Term<T> index_scan(string tableName, string indexName, Term<T> predicate);

    /// <summary>
    ///     分区扫描。
    /// </summary>
    [Operator("partition_scan")]
    Term<T> partition_scan(string tableName, IReadOnlyList<Guid> partitions);

    /// <summary>
    ///     空数据源。
    /// </summary>
    [Operator("empty_source")]
    Term<T> empty_source();

    /// <summary>
    ///     列投影。
    /// </summary>
    [Operator("project")]
    Term<T> project(Term<T> child, IReadOnlyList<string> columns);

    /// <summary>
    ///     表达式投影。
    /// </summary>
    [Operator("project_expr")]
    Term<T> project_expr(Term<T> child, IReadOnlyList<(SqlExpression Expr, string? Alias)> columns);

    /// <summary>
    ///     星号投影。
    /// </summary>
    [Operator("star_project")]
    Term<T> star_project(Term<T> child);

    /// <summary>
    ///     条件过滤。
    /// </summary>
    [Operator("filter")]
    Term<T> filter(Term<T> child, SqlExpression predicate);

    /// <summary>
    ///     WHERE 子句过滤。
    /// </summary>
    [Operator("where")]
    Term<T> where(Term<T> child, SqlExpression predicate);

    /// <summary>
    ///     哈希连接。
    /// </summary>
    [Operator("hash_join")]
    Term<T> hash_join(Term<T> left, Term<T> right, SqlExpression condition, JoinKind kind);

    /// <summary>
    ///     归并连接。
    /// </summary>
    [Operator("merge_join")]
    Term<T> merge_join(Term<T> left, Term<T> right, SqlExpression condition, JoinKind kind);

    /// <summary>
    ///     嵌套循环连接。
    /// </summary>
    [Operator("nested_loop_join")]
    Term<T> nested_loop_join(Term<T> left, Term<T> right, SqlExpression condition, JoinKind kind);

    /// <summary>
    ///     交叉连接。
    /// </summary>
    [Operator("cross_join")]
    Term<T> cross_join(Term<T> left, Term<T> right);

    /// <summary>
    ///     哈希聚合。
    /// </summary>
    [Operator("hash_aggregate")]
    Term<T> hash_aggregate(Term<T> child, IReadOnlyList<SqlExpression> keys,
        IReadOnlyList<(string Func, SqlExpression Arg, string? Alias)> aggregates);

    /// <summary>
    ///     预聚合。
    /// </summary>
    [Operator("pre_aggregate")]
    Term<T> pre_aggregate(Term<T> child, IReadOnlyList<SqlExpression> partialKeys,
        IReadOnlyList<(string Func, SqlExpression Arg)> partialAggs);

    /// <summary>
    ///     排序。
    /// </summary>
    [Operator("sort")]
    Term<T> sort(Term<T> child, IReadOnlyList<OrderByItem> orderBy);

    /// <summary>
    ///     Top-K 查询。
    /// </summary>
    [Operator("top_k")]
    Term<T> top_k(Term<T> child, int k, IReadOnlyList<OrderByItem> orderBy);

    /// <summary>
    ///     分页限制。
    /// </summary>
    [Operator("limit")]
    Term<T> limit(Term<T> child, int count, int offset);
}