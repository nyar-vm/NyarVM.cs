using Nargo.Cli.ProjectModel;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo graph 命令：显示工程图、依赖图、入口图、构建图
/// </summary>
public static class GraphCommand
{
    /// <summary>
    ///     显示工程图
    /// </summary>
    /// <param name="project">目标项目</param>
    public static void show_project(NargoProject project)
    {
        Console.WriteLine($"项目: {project.Name}@{project.Version}");
        Console.WriteLine($"  目标: {project.PrimaryTarget}");
        Console.WriteLine($"  来源: {project.CompatibilitySource}");

        if (project.Entries.Count > 0)
        {
            Console.WriteLine("  入口:");
            foreach (var entry in project.Entries)
            {
                var autoTag = entry.AutoDiscovered ? " (自动发现)" : "";
                Console.WriteLine($"    - {entry.Name}: {entry.Path} [{entry.Kind}]{autoTag}");
            }
        }

        if (project.Dependencies.Count > 0)
        {
            Console.WriteLine($"  依赖: {project.Dependencies.Count} 个");
        }

        if (project.DevDependencies.Count > 0)
        {
            Console.WriteLine($"  开发依赖: {project.DevDependencies.Count} 个");
        }
    }

    /// <summary>
    ///     显示工作区图
    /// </summary>
    /// <param name="workspace">目标工作区</param>
    public static void show_workspace(NargoWorkspace workspace)
    {
        Console.WriteLine($"工作区: {workspace.Name}");
        Console.WriteLine($"  来源: {workspace.CompatibilitySource}");
        Console.WriteLine($"  成员: {workspace.Members.Count} 个");

        foreach (var member in workspace.Members)
        {
            Console.WriteLine($"    - {member.Name}@{member.Version} ({member.PrimaryTarget})");
        }
    }

    /// <summary>
    ///     显示依赖图
    /// </summary>
    /// <param name="graph">依赖图</param>
    public static void show_dependency_graph(NargoDependencyGraph graph)
    {
        Console.WriteLine("依赖图:");
        Console.WriteLine("  内部依赖:");
        foreach (var (project, deps) in graph.InternalDependencies)
        {
            Console.WriteLine($"    {project} → [{string.Join(", ", deps)}]");
        }

        Console.WriteLine("  外部依赖:");
        foreach (var (project, deps) in graph.ExternalDependencies)
        {
            Console.WriteLine($"    {project} → {deps.Count} 个外部包");
        }

        var topoOrder = graph.get_topological_order();
        if (topoOrder.Count > 0)
        {
            Console.WriteLine($"  拓扑顺序: {string.Join(" → ", topoOrder)}");
        }
    }
}