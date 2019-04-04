using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     除法否定：Div(Neg(a), b) → Neg(Div(a, b))
/// </summary>
public sealed class DivNegRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "div-neg";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Div div) continue;

            var leftClass = egraph.get_class(div.left);
            var rightClass = egraph.get_class(div.right);

            if (leftClass is not null)
                foreach (var leftNode in leftClass.nodes)
                    if (leftNode is Neg negLeft)
                    {
                        var newDiv = egraph.add(new Div(negLeft.operand, div.right)).value;
                        yield return (node, new Neg(newDiv));
                    }

            if (rightClass is not null)
                foreach (var rightNode in rightClass.nodes)
                    if (rightNode is Neg negRight)
                    {
                        var newDiv = egraph.add(new Div(div.left, negRight.operand)).value;
                        yield return (node, new Neg(newDiv));
                    }
        }
    }
}