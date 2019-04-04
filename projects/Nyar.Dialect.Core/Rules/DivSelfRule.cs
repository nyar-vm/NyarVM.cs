using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     自身相除归一：Div(a, a) → Constant(1)（当 a ≠ 0）
/// </summary>
public sealed class DivSelfRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "div-self";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Div div) continue;

            if (IsZero(egraph, div.right)) continue;

            if (AreSameRoot(egraph, div.left, div.right)) yield return (node, new Literal<long>(1));
        }
    }

    private static bool AreSameRoot(EGraph<AlgebraNode> egraph, Id a, Id b)
    {
        return egraph.union_find.find(a).value == egraph.union_find.find(b).value;
    }

    private static bool IsZero(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        return eclass?.nodes.Any(n => n is Literal<long> { value: 0 }) ?? false;
    }
}