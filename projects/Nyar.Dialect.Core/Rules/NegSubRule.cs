using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     否定减法：Neg(Sub(a, b)) → Sub(b, a)
/// </summary>
public sealed class NegSubRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "neg-sub";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Neg neg) continue;

            var innerClass = egraph.get_class(neg.operand);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
                if (innerNode is Sub sub)
                    yield return (node, new Sub(sub.right, sub.left));
        }
    }
}