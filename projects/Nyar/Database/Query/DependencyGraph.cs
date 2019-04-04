using System.Collections;

namespace Nyar.Database.Query;

/// <summary>
///     依赖图，追踪查询间的依赖关系
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<QueryKey, HashSet<QueryKey>> _dependencies = [];
    private readonly Dictionary<QueryKey, HashSet<QueryKey>> _dependents = [];

    /// <summary>
    ///     添加依赖关系：fromKey 依赖 onKey
    /// </summary>
    /// <param name="fromKey">依赖方查询键。</param>
    /// <param name="onKey">被依赖方查询键。</param>
    public void add_dependency(QueryKey fromKey, QueryKey onKey)
    {
        if (!_dependencies.TryGetValue(fromKey, out var deps))
        {
            deps = [];
            _dependencies[fromKey] = deps;
        }

        deps.Add(onKey);

        if (!_dependents.TryGetValue(onKey, out var dependents))
        {
            dependents = [];
            _dependents[onKey] = dependents;
        }

        dependents.Add(fromKey);
    }

    /// <summary>
    ///     获取指定查询直接依赖的所有查询键
    /// </summary>
    /// <param name="key">查询键。</param>
    /// <returns>依赖的查询键集合。</returns>
    public IReadOnlySet<QueryKey> get_dependencies(QueryKey key)
    {
        if (_dependencies.TryGetValue(key, out var deps)) return deps;

        return ReadOnlySet<QueryKey>.empty;
    }

    /// <summary>
    ///     获取指定查询的所有依赖方（受影响方），包括传递依赖
    /// </summary>
    /// <param name="key">查询键。</param>
    /// <returns>所有受影响的查询键集合。</returns>
    public IReadOnlySet<QueryKey> get_transitive_dependents(QueryKey key)
    {
        var result = new HashSet<QueryKey>();
        var stack = new Stack<QueryKey>();
        stack.Push(key);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (_dependents.TryGetValue(current, out var dependents))
                foreach (var dep in dependents)
                    if (result.Add(dep))
                        stack.Push(dep);
        }

        return result;
    }

    /// <summary>
    ///     移除指定查询的所有依赖关系
    /// </summary>
    /// <param name="key">查询键。</param>
    public void remove_all_dependencies(QueryKey key)
    {
        if (_dependencies.TryGetValue(key, out var deps))
        {
            foreach (var dep in deps)
                if (_dependents.TryGetValue(dep, out var dependents))
                    dependents.Remove(key);

            _dependencies.Remove(key);
        }
    }

    /// <summary>
    ///     清空所有依赖关系
    /// </summary>
    public void clear()
    {
        _dependencies.Clear();
        _dependents.Clear();
    }
}

/// <summary>
///     只读空集合
/// </summary>
internal sealed class ReadOnlySet<T> : IReadOnlySet<T>
{
    /// <summary>
    ///     空集合单例
    /// </summary>
    public static ReadOnlySet<T> empty { get; } = new();

    /// <summary>
    ///     集合元素数量
    /// </summary>
    public int Count => 0;

    /// <summary>
    ///     获取枚举器
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        return Enumerable.Empty<T>().GetEnumerator();
    }

    /// <summary>
    ///     非泛型枚举器
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///     判断是否包含指定元素
    /// </summary>
    public bool Contains(T item)
    {
        return false;
    }

    /// <summary>
    ///     判断是否为指定集合的真子集
    /// </summary>
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        return !other.Any();
    }

    /// <summary>
    ///     判断是否为指定集合的真超集
    /// </summary>
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        return false;
    }

    /// <summary>
    ///     判断是否为指定集合的子集
    /// </summary>
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        return true;
    }

    /// <summary>
    ///     判断是否为指定集合的超集
    /// </summary>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        return !other.Any();
    }

    /// <summary>
    ///     判断是否与指定集合有交集
    /// </summary>
    public bool Overlaps(IEnumerable<T> other)
    {
        return false;
    }

    /// <summary>
    ///     判断是否与指定集合相等
    /// </summary>
    public bool SetEquals(IEnumerable<T> other)
    {
        return !other.Any();
    }
}