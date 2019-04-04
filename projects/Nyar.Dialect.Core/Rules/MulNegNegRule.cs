using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     双重否定乘法：Mul(Neg(a), Neg(b)) → Mul(a, b)
/// </summary>
public sealed class MulNegNegRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "mul-neg-neg";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Mul mul) continue;

            var leftClass = egraph.get_class(mul.left);
            var rightClass = egraph.get_class(mul.right);
            if (leftClass is null || rightClass is null) continue;

            foreach (var leftNode in leftClass.nodes)
            {
                if (leftNode is not Neg negLeft) continue;

                foreach (var rightNode in rightClass.nodes)
                    if (rightNode is Neg negRight)
                        yield return (node, new Mul(negLeft.operand, negRight.operand));
            }
        }
    }
}