namespace Nyar.Database.Query;

/// <summary>
///     查询注册表，管理所有已注册的查询
/// </summary>
public sealed class QueryRegistry
{
    private readonly Dictionary<string, IQuery> _queries = [];

    /// <summary>
    ///     获取所有已注册查询的名称
    /// </summary>
    public IReadOnlyList<string> registered_names => _queries.Keys.ToList().AsReadOnly();

    /// <summary>
    ///     注册查询
    /// </summary>
    /// <param name="query">查询实例。</param>
    /// <exception cref="InvalidOperationException">同名查询已注册</exception>
    public void register(IQuery query)
    {
        if (!_queries.TryAdd(query.name, query))
            throw new InvalidOperationException($"Query '{query.name}' is already registered.");
    }

    /// <summary>
    ///     获取指定名称的查询
    /// </summary>
    /// <typeparam name="TInput">查询输入类型</typeparam>
    /// <typeparam name="TOutput">查询输出类型</typeparam>
    /// <param name="name">查询名称。</param>
    /// <returns>查询实例。</returns>
    /// <exception cref="KeyNotFoundException">查询未注册</exception>
    public IQuery<TInput, TOutput> get<TInput, TOutput>(string name)
    {
        if (_queries.TryGetValue(name, out var query) && query is IQuery<TInput, TOutput> typed) return typed;

        throw new KeyNotFoundException($"Query '{name}' is not registered or type mismatch.");
    }

    /// <summary>
    ///     尝试获取指定名称的查询
    /// </summary>
    /// <typeparam name="TInput">查询输入类型</typeparam>
    /// <typeparam name="TOutput">查询输出类型</typeparam>
    /// <param name="name">查询名称。</param>
    /// <param name="query">查询实例。</param>
    /// <returns>是否获取成功。</returns>
    public bool try_get<TInput, TOutput>(string name, out IQuery<TInput, TOutput>? query)
    {
        if (_queries.TryGetValue(name, out var q) && q is IQuery<TInput, TOutput> typed)
        {
            query = typed;
            return true;
        }

        query = default;
        return false;
    }
}