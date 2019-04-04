namespace Nyar.VM.NyarVM.HotReload;

/// <summary>
///     模块依赖图，跟踪模块间的导入关系
///     支持级联重载：当被依赖模块更新时，自动标记依赖方需要重载
/// </summary>
public sealed class ModuleDependencyGraph
{
    /// <summary>
    ///     依赖关系：模块名 → 它所依赖的模块名集合
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();

    /// <summary>
    ///     反向依赖：模块名 → 依赖它的模块名集合
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _dependents = new();

    /// <summary>
    ///     注册模块的依赖关系
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <param name="imports">该模块导入的其他模块名列表。</param>
    public void register_dependencies(string moduleName, IEnumerable<string> imports)
    {
        if (!_dependencies.ContainsKey(moduleName)) _dependencies[moduleName] = [];

        foreach (var import in imports)
        {
            _dependencies[moduleName].Add(import);

            if (!_dependents.ContainsKey(import)) _dependents[import] = [];

            _dependents[import].Add(moduleName);
        }
    }

    /// <summary>
    ///     移除模块的所有依赖关系
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    public void remove_module(string moduleName)
    {
        if (_dependencies.TryGetValue(moduleName, out var deps))
        {
            foreach (var dep in deps)
                if (_dependents.TryGetValue(dep, out var dependents))
                    dependents.Remove(moduleName);

            _dependencies.Remove(moduleName);
        }

        _dependents.Remove(moduleName);
    }

    /// <summary>
    ///     获取直接依赖指定模块的所有模块（反向依赖）
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>依赖该模块的模块名集合。</returns>
    public IReadOnlySet<string> get_dependents(string moduleName)
    {
        return _dependents.GetValueOrDefault(moduleName) ?? [];
    }

    /// <summary>
    ///     获取模块的所有传递依赖方（递归查找）
    ///     用于级联重载
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>所有传递依赖该模块的模块名集合。</returns>
    public IReadOnlySet<string> get_transitive_dependents(string moduleName)
    {
        var result = new HashSet<string>();
        var queue = new Queue<string>();

        foreach (var dep in get_dependents(moduleName)) queue.Enqueue(dep);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (result.Add(current))
                foreach (var dep in get_dependents(current))
                    if (!result.Contains(dep))
                        queue.Enqueue(dep);
        }

        return result;
    }

    /// <summary>
    ///     获取模块的直接依赖
    /// </summary>
    /// <param name="moduleName">模块名称。</param>
    /// <returns>该模块依赖的模块名集合。</returns>
    public IReadOnlySet<string> get_dependencies(string moduleName)
    {
        return _dependencies.GetValueOrDefault(moduleName) ?? [];
    }

    /// <summary>
    ///     检查是否存在循环依赖
    /// </summary>
    /// <param name="moduleName">起始模块。</param>
    /// <returns>是否存在循环依赖。</returns>
    public bool has_circular_dependency(string moduleName)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        return dfs_check(moduleName, visited, recursionStack);
    }

    private bool dfs_check(string current, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(current)) return true;

        if (!visited.Add(current)) return false;

        recursionStack.Add(current);

        foreach (var dep in get_dependencies(current))
            if (dfs_check(dep, visited, recursionStack))
                return true;

        recursionStack.Remove(current);
        return false;
    }
}