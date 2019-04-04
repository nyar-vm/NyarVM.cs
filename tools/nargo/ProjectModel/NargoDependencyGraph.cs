namespace Nargo.Cli.ProjectModel;

/// <summary>
///     Nargo 依赖图：表达项目间的依赖关系
/// </summary>
public sealed class NargoDependencyGraph
{
    /// <summary>
    ///     内部依赖关系（项目名 → 依赖的项目名列表）
    /// </summary>
    public Dictionary<string, List<string>> InternalDependencies { get; init; } = new();

    /// <summary>
    ///     外部依赖关系（项目名 → 外部包名列表）
    /// </summary>
    public Dictionary<string, List<string>> ExternalDependencies { get; init; } = new();

    /// <summary>
    ///     获取拓扑排序后的项目构建顺序
    /// </summary>
    /// <returns>按依赖顺序排列的项目名列表</returns>
    public List<string> get_topological_order()
    {
        var visited = new HashSet<string>();
        var result = new List<string>();
        var visiting = new HashSet<string>();

        foreach (var node in InternalDependencies.Keys)
        {
            visit(node, visited, visiting, result);
        }

        return result;
    }

    /// <summary>
    ///     深度优先遍历实现拓扑排序
    /// </summary>
    private void visit(string node, HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visited.Contains(node))
        {
            return;
        }

        if (visiting.Contains(node))
        {
            return;
        }

        visiting.Add(node);

        if (InternalDependencies.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
            {
                visit(dep, visited, visiting, result);
            }
        }

        visiting.Remove(node);
        visited.Add(node);
        result.Add(node);
    }
}