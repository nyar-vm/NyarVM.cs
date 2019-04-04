using Std.Data.Text.Sql;

namespace Nyar.Dialect.Query;

/// <summary>
///     JOIN 类型枚举
/// </summary>
public enum JoinKind
{
    inner,
    left,
    right,
    full,
    cross,
    semi,
    anti
}

/// <summary>
///     表扫描代数接口
/// </summary>
public interface IScanAlgebra<T>
{
    /// <summary>
    ///     全表扫描
    /// </summary>
    T table_scan(string table, string? alias);

    /// <summary>
    ///     索引扫描
    /// </summary>
    T index_scan(string table, string indexName, T predicate);

    /// <summary>
    ///     分区扫描
    /// </summary>
    T partition_scan(string table, IReadOnlyList<Guid> partitions);

    /// <summary>
    ///     空数据源
    /// </summary>
    T empty_source();
}

/// <summary>
///     投影代数接口
/// </summary>
public interface IProjectAlgebra<T>
{
    /// <summary>
    ///     列投影
    /// </summary>
    T project(T child, IReadOnlyList<string> columns);

    /// <summary>
    ///     表达式投影
    /// </summary>
    T project_expr(T child, IReadOnlyList<(SqlExpression Expr, string? Alias)> columns);

    /// <summary>
    ///     星号投影
    /// </summary>
    T star_project(T child);
}

/// <summary>
///     过滤代数接口
/// </summary>
public interface IFilterAlgebra<T>
{
    /// <summary>
    ///     条件过滤
    /// </summary>
    T filter(T child, SqlExpression predicate);

    /// <summary>
    ///     WHERE 子句过滤
    /// </summary>
    T where(T child, SqlExpression predicate);
}

/// <summary>
///     JOIN 代数接口
/// </summary>
public interface IJoinAlgebra<T>
{
    /// <summary>
    ///     哈希连接
    /// </summary>
    T hash_join(T left, T right, SqlExpression condition, JoinKind kind);

    /// <summary>
    ///     归并连接
    /// </summary>
    T merge_join(T left, T right, SqlExpression condition, JoinKind kind);

    /// <summary>
    ///     嵌套循环连接
    /// </summary>
    T nested_loop_join(T left, T right, SqlExpression condition, JoinKind kind);

    /// <summary>
    ///     交叉连接
    /// </summary>
    T cross_join(T left, T right);
}

/// <summary>
///     聚合代数接口
/// </summary>
public interface IAggregateAlgebra<T>
{
    /// <summary>
    ///     哈希聚合
    /// </summary>
    T hash_aggregate(T child, IReadOnlyList<SqlExpression> keys,
        IReadOnlyList<(string Func, SqlExpression Arg, string? Alias)> aggregates);

    /// <summary>
    ///     预聚合
    /// </summary>
    T pre_aggregate(T child, IReadOnlyList<SqlExpression> partialKeys,
        IReadOnlyList<(string Func, SqlExpression Arg)> partialAggs);
}

/// <summary>
///     排序代数接口
/// </summary>
public interface IOrderAlgebra<T>
{
    /// <summary>
    ///     排序
    /// </summary>
    T sort(T child, IReadOnlyList<OrderByItem> orderBy);

    /// <summary>
    ///     Top-K 查询
    /// </summary>
    T top_k(T child, int k, IReadOnlyList<OrderByItem> orderBy);

    /// <summary>
    ///     分页限制
    /// </summary>
    T limit(T child, int count, int offset);
}

/// <summary>
///     查询代数聚合接口，组合所有查询代数子接口。
///     保留该旧接口以兼容现有调用方，并与新的独立方言接口对齐。
/// </summary>
public interface IQueryAlgebra<T> : IQuery<T>, IScanAlgebra<T>, IProjectAlgebra<T>, IFilterAlgebra<T>, IJoinAlgebra<T>,
    IAggregateAlgebra<T>, IOrderAlgebra<T>
{
}