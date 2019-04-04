using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     减法恒等：Sub(a, 0) → a
/// </summary>
public sealed class SubZeroRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sub-zero";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Sub sub) continue;

            if (IsZero(egraph, sub.right))
                if (TryGetReplacement(egraph, sub.left, out var rep))
                    yield return (node, rep);
        }
    }

    /// <summary>
    ///     从指定 Id 的等价类中获取替换节点，直接使用等价类中的现有节点以触发 EGraph 合并
    /// </summary>
    private static bool TryGetReplacement(EGraph<AlgebraNode> egraph, Id id, out AlgebraNode replacement)
    {
        replacement = null!;
        var targetClass = egraph.get_class(id);
        if (targetClass is null || targetClass.nodes.Count == 0) return false;

        // 优先返回 OA 节点，次选旧类型
        replacement = targetClass.nodes.FirstOrDefault(n => n is Literal<long>)
                      ?? targetClass.nodes.First();
        return true;
    }

    private static bool IsZero(EGraph<AlgebraNode> egraph, Id id)
    {
        var targetClass = egraph.get_class(id);
        return targetClass?.nodes.Any(n => n is Literal<long> { value: 0 }) ?? false;
    }
}