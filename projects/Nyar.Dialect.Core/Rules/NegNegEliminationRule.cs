using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     双重否定消除：Neg(Neg(x)) → x
/// </summary>
public sealed class NegNegEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "neg-neg-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Neg outerNeg) continue;

            var innerClass = egraph.get_class(outerNeg.operand);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
                if (innerNode is Neg innerNeg)
                    if (TryGetReplacement(egraph, innerNeg.operand, out var rep))
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
}