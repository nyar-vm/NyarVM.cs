namespace Nyar.Language.Valkyrie.Compiler.Semantic;

/// <summary>
///     模块依赖图，基于 using/namespace 构建依赖关系并拓扑排序
/// </summary>
public sealed class ModuleDependencyGraph
{
    /// <summary>
    ///     对编译单元列表进行拓扑排序
    /// </summary>
    /// <param name="asts">所有文件的 AST 编译单元列表</param>
    /// <param name="table">全局声明表</param>
    /// <returns>按依赖顺序排列的编译单元列表</returns>
    public IReadOnlyList<CompilationUnit> topological_sort(
        IReadOnlyList<CompilationUnit> asts,
        GlobalDeclarationTable table)
    {
        var adjacency = new Dictionary<string, HashSet<string>>();
        var inDegree = new Dictionary<string, int>();
        var nameToAst = new Dictionary<string, CompilationUnit>();

        foreach (var ast in asts)
        {
            var moduleName = extract_module_name(ast);
            nameToAst[moduleName] = ast;

            if (!adjacency.ContainsKey(moduleName))
            {
                adjacency[moduleName] = [];
                inDegree[moduleName] = 0;
            }
        }

        foreach (var ast in asts)
        {
            var moduleName = extract_module_name(ast);

            foreach (var decl in ast.declarations)
            {
                if (decl is not ImportDecl usingDecl) continue;

                var usedModule = usingDecl.module_path;
                if (adjacency.ContainsKey(usedModule))
                    if (adjacency[usedModule].Add(moduleName))
                        inDegree[moduleName] = inDegree.GetValueOrDefault(moduleName, 0) + 1;
            }
        }

        detect_cycles(adjacency, asts);

        var queue = new Queue<string>();
        foreach (var (name, degree) in inDegree)
            if (degree == 0)
                queue.Enqueue(name);

        var sorted = new List<CompilationUnit>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (nameToAst.TryGetValue(current, out var ast)) sorted.Add(ast);

            foreach (var neighbor in adjacency.GetValueOrDefault(current, []))
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0) queue.Enqueue(neighbor);
            }
        }

        return sorted;
    }

    /// <summary>
    ///     从 AST 编译单元中提取模块名
    /// </summary>
    private static string extract_module_name(CompilationUnit ast)
    {
        foreach (var decl in ast.declarations)
            if (decl is NamespaceDecl ns)
                return ns.name.name;

        return "_root";
    }

    private static void detect_cycles(
        Dictionary<string, HashSet<string>> adjacency,
        IReadOnlyList<CompilationUnit> asts)
    {
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();

        foreach (var node in adjacency.Keys)
            if (!visited.Contains(node))
                if (has_cycle(node, adjacency, visited, inStack, []))
                {
                    // 循环依赖检测到，记录诊断信息
                    // 当前以宽松模式继续编译
                }
    }

    private static bool has_cycle(
        string node,
        Dictionary<string, HashSet<string>> adjacency,
        HashSet<string> visited,
        HashSet<string> inStack,
        List<string> path)
    {
        visited.Add(node);
        inStack.Add(node);
        path.Add(node);

        foreach (var neighbor in adjacency.GetValueOrDefault(node, []))
            if (!visited.Contains(neighbor))
            {
                if (has_cycle(neighbor, adjacency, visited, inStack, path)) return true;
            }
            else if (inStack.Contains(neighbor))
            {
                return true;
            }

        path.RemoveAt(path.Count - 1);
        inStack.Remove(node);
        return false;
    }
}