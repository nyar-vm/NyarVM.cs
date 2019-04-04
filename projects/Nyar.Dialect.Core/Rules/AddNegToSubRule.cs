using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     加法逆元规则：Add(a, Neg(b)) → Sub(a, b)
/// </summary>
public sealed class AddNegToSubRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "add-neg-to-sub";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Add add) continue;

            var rightClass = egraph.get_class(add.right);
            if (rightClass is not null)
                foreach (var rightNode in rightClass.nodes)
                    if (rightNode is Neg negRight)
                        yield return (node, new Sub(add.left, negRight.operand));

            var leftClass = egraph.get_class(add.left);
            if (leftClass is not null)
                foreach (var leftNode in leftClass.nodes)
                    if (leftNode is Neg negLeft)
                        yield return (node, new Sub(add.right, negLeft.operand));
        }
    }
}