using System.Linq.Expressions;
using Hermes.YYDB.Query;

namespace Hermes.ORM;

/// <summary>
///     查询扩展方法
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    ///     从查询执行器创建类型化查询构建器
    /// </summary>
    public static TypedQueryBuilder<T> Query<T>(this IQueryExecutor executor) where T : class
    {
        return new TypedQueryBuilder<T>(executor);
    }
}

/// <summary>
///     类型化查询构建器
/// </summary>
public sealed class TypedQueryBuilder<T> where T : class
{
    private readonly List<FieldAssignment> _assignments = [];
    private readonly IQueryExecutor _executor;
    private QueryOrdering? _ordering;
    private QueryPagination? _pagination;
    private QueryPredicate? _predicate;
    private int? _skip;
    private int? _take;

    /// <summary>
    ///     初始化 <see cref="TypedQueryBuilder{T}" /> 类的新实例
    /// </summary>
    public TypedQueryBuilder(IQueryExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    ///     添加过滤条件
    /// </summary>
    public TypedQueryBuilder<T> Filter(Expression<Func<T, bool>> predicate)
    {
        var extracted = PredicateExtractor.Extract(predicate.Body);
        _predicate = _predicate == null ? extracted : new AndPredicate(_predicate, extracted);
        return this;
    }

    /// <summary>
    ///     设置排序规则
    /// </summary>
    public TypedQueryBuilder<T> Sort<TKey>(Expression<Func<T, TKey>> keySelector, bool descending = false)
    {
        var fieldName = ExpressionHelper.GetFieldName(keySelector.Body);
        _ordering = new QueryOrdering(fieldName, descending);
        return this;
    }

    /// <summary>
    ///     设置字段赋值
    /// </summary>
    public TypedQueryBuilder<T> Set<TValue>(Expression<Func<T, TValue>> fieldSelector, TValue value)
    {
        var fieldName = ExpressionHelper.GetFieldName(fieldSelector.Body);
        _assignments.Add(new FieldAssignment(fieldName, value!));
        return this;
    }

    /// <summary>
    ///     设置跳过行数
    /// </summary>
    public TypedQueryBuilder<T> Skip(int offset)
    {
        _skip = offset;
        return this;
    }

    /// <summary>
    ///     设置获取行数
    /// </summary>
    public TypedQueryBuilder<T> Take(int limit)
    {
        _take = limit;
        return this;
    }

    /// <summary>
    ///     执行查询并返回列表
    /// </summary>
    public async Task<List<T>> ToListAsync(CancellationToken ct = default)
    {
        _pagination = new QueryPagination(_skip ?? 0, _take ?? 1000);
        var query = new FindQuery(typeof(T).Name, _predicate, _ordering, _pagination);
        var result = await _executor.ExecuteAsync(query, ct);
        if (!result.Success) throw new InvalidOperationException($"查询失败：{result.Error}");

        return ResultMapper.Map<T>(result.Rows);
    }

    /// <summary>
    ///     执行查询并返回第一条结果
    /// </summary>
    public async Task<T?> FirstOrDefaultAsync(CancellationToken ct = default)
    {
        _pagination = new QueryPagination(0, 1);
        var query = new FindQuery(typeof(T).Name, _predicate, _ordering, _pagination);
        var result = await _executor.ExecuteAsync(query, ct);
        if (!result.Success) throw new InvalidOperationException($"查询失败：{result.Error}");

        var items = ResultMapper.Map<T>(result.Rows);
        return items.Count > 0 ? items[0] : default;
    }

    /// <summary>
    ///     执行计数查询
    /// </summary>
    public async Task<long> CountAsync(CancellationToken ct = default)
    {
        var query = new AggregateQuery(typeof(T).Name, [new AggregateOperation(AggregateFunction.Count, "*", "count")],
            _predicate);
        var result = await _executor.ExecuteAsync(query, ct);
        if (!result.Success) throw new InvalidOperationException($"查询失败：{result.Error}");

        if (result.Rows.Count > 0 && result.Rows[0].TryGetValue("count", out var val)) return Convert.ToInt64(val);

        return 0;
    }

    /// <summary>
    ///     执行存在性查询
    /// </summary>
    public async Task<bool> AnyAsync(CancellationToken ct = default)
    {
        _pagination = new QueryPagination(0, 1);
        var query = new FindQuery(typeof(T).Name, _predicate, null, _pagination);
        var result = await _executor.ExecuteAsync(query, ct);
        return result is { Success: true, Rows.Count: > 0 };
    }

    /// <summary>
    ///     执行插入操作
    /// </summary>
    public async Task InsertAsync(T entity, CancellationToken ct = default)
    {
        var assignments = EntityMapper.ToAssignments(entity);
        var query = new CreateQuery(typeof(T).Name, assignments);
        var result = await _executor.ExecuteAsync(query, ct);
        if (!result.Success) throw new InvalidOperationException($"插入失败：{result.Error}");
    }

    /// <summary>
    ///     执行更新操作
    /// </summary>
    public async Task UpdateAsync(CancellationToken ct = default)
    {
        if (_predicate == null) throw new InvalidOperationException("更新操作必须指定 Filter 条件");

        if (_assignments.Count == 0) throw new InvalidOperationException("更新操作必须指定至少一个 Set 赋值");

        var query = new UpdateQuery(typeof(T).Name, _predicate, _assignments);
        var result = await _executor.ExecuteAsync(query, ct);
        if (!result.Success) throw new InvalidOperationException($"更新失败：{result.Error}");
    }

    /// <summary>
    ///     执行删除操作
    /// </summary>
    public async Task DeleteAsync(CancellationToken ct = default)
    {
        if (_predicate == null) throw new InvalidOperationException("删除操作必须指定 Filter 条件");

        var query = new DeleteQuery(typeof(T).Name, _predicate);
        var result = await _executor.ExecuteAsync(query, ct);
        if (!result.Success) throw new InvalidOperationException($"删除失败：{result.Error}");
    }
}