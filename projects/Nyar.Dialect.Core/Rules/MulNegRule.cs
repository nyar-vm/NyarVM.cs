using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     乘法否定：Mul(Neg(a), b) → Neg(Mul(a, b))
/// </summary>
public sealed class MulNegRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "mul-neg";

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

            if (leftClass is not null)
                foreach (var leftNode in leftClass.nodes)
                    if (leftNode is Neg negLeft)
                    {
                        var newMul = egraph.add(new Mul(negLeft.operand, mul.right)).value;
                        yield return (node, new Neg(newMul));
                    }

            if (rightClass is not null)
                foreach (var rightNode in rightClass.nodes)
                    if (rightNode is Neg negRight)
                    {
                        var newMul = egraph.add(new Mul(mul.left, negRight.operand)).value;
                        yield return (node, new Neg(newMul));
                    }
        }
    }
}