using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;

namespace Nyar.IR.Rewrite;

/// <summary>
///     基于开放模式匹配的重写规则。
///     匹配特定 ENode 模式并生成等价 ENode。
///     不依赖封闭节点宇宙。
/// </summary>
/// <typeparam name="T">节点类型。</typeparam>
public class RewriteRule<T> : IRewriteRule<T> where T : ILanguage<T>
{
    private readonly Func<T, bool> _matcher;
    private readonly Func<EGraph<T>, T, Id?> _rewriter;

    /// <summary>
    ///     创建 RewriteRule 实例
    /// </summary>
    /// <param name="name">规则名称。</param>
    /// <param name="matcher">节点匹配器。</param>
    /// <param name="rewriter">重写函数。</param>
    public RewriteRule(string name, Func<T, bool> matcher, Func<EGraph<T>, T, Id?> rewriter)
    {
        this.name = name;
        _matcher = matcher;
        _rewriter = rewriter;
    }

    /// <summary>
    ///     规则名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     创建基于模式匹配和替换的重写规则
    /// </summary>
    /// <param name="name">规则名称。</param>
    /// <param name="pattern">要匹配的模式节点。</param>
    /// <param name="replacement">替换节点。</param>
    public static RewriteRule<T> create(string name, T pattern, T replacement)
    {
        return new RewriteRule<T>(
            name,
            node => match_pattern(node, pattern),
            (egraph, node) =>
            {
                var replacementId = egraph.add(replacement);
                return replacementId;
            });
    }

    /// <summary>
    ///     对 E-Graph 中的每个等价类应用此规则
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <returns>是否产生了新的等价关系。</returns>
    public bool apply(EGraph<T> egraph)
    {
        var changed = false;

        var snapshot = new List<(Id ClassId, List<T> Nodes)>();
        foreach (var (classId, eclass) in egraph.classes) snapshot.Add((eclass.id, [.. eclass.nodes]));

        foreach (var (classId, nodes) in snapshot)
        {
            var currentRoot = egraph.union_find.find(classId);

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!_matcher(node)) continue;

                var newId = _rewriter(egraph, node);
                if (newId is null) continue;

                var canonicalNew = egraph.union_find.find(newId.Value);
                var existingId = new Id(canonicalNew.value);

                if (existingId.value != currentRoot.value)
                {
                    egraph.union(currentRoot, existingId);
                    changed = true;
                    break;
                }
            }
        }

        return changed;
    }

    /// <summary>
    ///     测试节点是否匹配此规则的模式
    /// </summary>
    /// <param name="node">待测试节点。</param>
    /// <returns>是否匹配。</returns>
    public bool matches(T node)
    {
        return _matcher(node);
    }

    private static bool match_pattern(T node, T pattern)
    {
        // 基础模式匹配：比较 operator key 和 children 结构
        if (node is ENode nodeEnode && pattern is ENode patternEnode)
        {
            if (!nodeEnode.@operator.key.Equals(patternEnode.@operator.key)) return false;

            if (nodeEnode.children.Length != patternEnode.children.Length) return false;

            for (var i = 0; i < nodeEnode.children.Length; i++)
                // 子节点只需要有相同的 Id 结构即可
                if (!nodeEnode.children[i].Equals(patternEnode.children[i]))
                    return false;

            return true;
        }

        return node.Equals(pattern);
    }
}