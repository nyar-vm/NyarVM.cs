namespace Nyar.Database.Query;

/// <summary>
///     查询执行引擎，协调缓存查找、依赖追踪和计算执行
/// </summary>
public sealed class QueryEngine
{
    /// <summary>
    ///     创建查询执行引擎
    /// </summary>
    /// <param name="registry">查询注册表。</param>
    /// <param name="cache">查询缓存。</param>
    /// <param name="dependencyGraph">依赖图。</param>
    public QueryEngine(QueryRegistry registry, IQueryCache cache, DependencyGraph dependencyGraph)
    {
        this.registry = registry;
        this.cache = cache;
        dependency_graph = dependencyGraph;
    }

    /// <summary>
    ///     查询注册表
    /// </summary>
    public QueryRegistry registry { get; }

    /// <summary>
    ///     查询缓存
    /// </summary>
    public IQueryCache cache { get; }

    /// <summary>
    ///     依赖图
    /// </summary>
    public DependencyGraph dependency_graph { get; }

    /// <summary>
    ///     执行查询，优先使用缓存
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TOutput">输出类型</typeparam>
    /// <param name="query">查询实例。</param>
    /// <param name="input">输入参数。</param>
    /// <returns>查询结果。</returns>
    public QueryResult<TOutput> execute<TInput, TOutput>(IQuery<TInput, TOutput> query, TInput input)
    {
        var key = QueryKey.create(query.name, input);
        var inputHash = query.compute_input_hash(input);

        var cached = cache.get<TOutput>(key, inputHash);
        if (cached is not null) return cached;

        var result = execute_query(query, input, null);
        var queryResult = QueryResult<TOutput>.success(result, inputHash);
        cache.set(key, queryResult);

        return queryResult;
    }

    /// <summary>
    ///     内部执行查询方法，由 QueryContext 调用依赖查询时使用
    /// </summary>
    internal TOutput execute_query<TInput, TOutput>(IQuery<TInput, TOutput> query, TInput input,
        QueryContext? parentContext)
    {
        var key = QueryKey.create(query.name, input);
        var inputHash = query.compute_input_hash(input);

        var cached = cache.get<TOutput>(key, inputHash);
        if (cached is not null) return cached.value!;

        dependency_graph.remove_all_dependencies(key);
        var context = new QueryContext(this, dependency_graph, key);
        var output = query.execute(input, context);

        var result = QueryResult<TOutput>.success(output, inputHash);
        cache.set(key, result);

        return output;
    }

    /// <summary>
    ///     使指定输入的查询缓存失效，并传播到所有依赖方
    /// </summary>
    /// <typeparam name="TInput">输入类型</typeparam>
    /// <typeparam name="TOutput">输出类型</typeparam>
    /// <param name="query">查询实例。</param>
    /// <param name="input">输入参数。</param>
    public void invalidate<TInput, TOutput>(IQuery<TInput, TOutput> query, TInput input)
    {
        var key = QueryKey.create(query.name, input);
        var transitiveDependents = dependency_graph.get_transitive_dependents(key);

        cache.invalidate(key);
        cache.invalidate_range(transitiveDependents);
    }

    /// <summary>
    ///     使所有缓存失效
    /// </summary>
    public void invalidate_all()
    {
        cache.clear();
        dependency_graph.clear();
    }
}