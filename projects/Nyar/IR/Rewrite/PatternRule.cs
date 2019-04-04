using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.IR.Rewrite;

/// <summary>
///     基于模式匹配的重写规则，匹配特定节点模式并生成等价节点
/// </summary>
/// <typeparam name="T">语言节点类型</typeparam>
public class PatternRule<T> : IRewriteRule<T> where T : ILanguage<T>
{
    private readonly Func<T, bool> _matcher;
    private readonly Func<EGraph<T>, T, Id?> _rewriter;

    /// <summary>
    ///     创建模式重写规则
    /// </summary>
    /// <param name="name">规则名称。</param>
    /// <param name="matcher">节点匹配器。</param>
    /// <param name="rewriter">重写函数，返回新节点所属等价类 Id，若不适用返回 null。</param>
    public PatternRule(string name, Func<T, bool> matcher, Func<EGraph<T>, T, Id?> rewriter)
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
    ///     对 E-Graph 中的每个等价类应用此规则
    /// </summary>
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
    ///     测试节点是否匹配此规则的匹配器
    /// </summary>
    /// <param name="node">待测试节点。</param>
    /// <returns>是否匹配。</returns>
    public bool matches(T node)
    {
        return _matcher(node);
    }

    /// <summary>
    ///     对匹配的节点应用重写器
    /// </summary>
    /// <param name="egraph">目标 E-Graph。</param>
    /// <param name="node">匹配的节点。</param>
    /// <returns>重写结果等价类 Id，若不适用则返回 null。</returns>
    public Id? rewrite(EGraph<T> egraph, T node)
    {
        return _rewriter(egraph, node);
    }
}