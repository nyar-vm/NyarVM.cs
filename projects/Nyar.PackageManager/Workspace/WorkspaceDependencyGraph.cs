namespace Nyar.PackageManager.Workspace;

public class WorkspaceDependencyGraph
{
    public Dictionary<string, List<string>> internal_dependencies { get; set; } = new();
    public Dictionary<string, List<string>> external_dependencies { get; set; } = new();

    public List<string> get_topological_order()
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var node in internal_dependencies.Keys) topological_visit(node, visited, visiting, result);

        return result;
    }

    private void topological_visit(string node, HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visited.Contains(node)) return;

        if (!visiting.Add(node)) return;

        if (internal_dependencies.TryGetValue(node, out var deps))
            foreach (var dep in deps)
                topological_visit(dep, visited, visiting, result);

        visiting.Remove(node);
        visited.Add(node);
        result.Add(node);
    }
}