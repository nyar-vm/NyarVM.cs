namespace Nyar.Database.Query;

/// <summary>
///     查询执行上下文，提供依赖查询的调用接口
/// </summary>
public sealed class QueryContext
{
    private readonly QueryKey _current_query_key;
    private readonly DependencyGraph _dependency_graph;
    private readonly QueryEngine _engine;

    /// <summary>
    ///     创建查询执行上下文
    /// </summary>
    internal QueryContext(QueryEngine engine, DependencyGraph dependencyGraph, QueryKey currentQueryKey)
    {
        _engine = engine;
        _dependency_graph = dependencyGraph;
        _current_query_key = currentQueryKey;
    }

    /// <summary>
    ///     调用依赖查询，自动记录依赖关系
    /// </summary>
    /// <typeparam name="TInput">依赖查询的输入类型</typeparam>
    /// <typeparam name="TOutput">依赖查询的输出类型</typeparam>
    /// <param name="query">依赖查询。</param>
    /// <param name="input">依赖查询的输入参数。</param>
    /// <returns>依赖查询的结果。</returns>
    public TOutput invoke<TInput, TOutput>(IQuery<TInput, TOutput> query, TInput input)
    {
        var dependencyKey = QueryKey.create(query.name, input);
        _dependency_graph.add_dependency(_current_query_key, dependencyKey);
        return _engine.execute_query(query, input, this);
    }
}