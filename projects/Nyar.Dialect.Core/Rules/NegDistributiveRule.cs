using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     否定分配律：Neg(Add(a, b)) → Add(Neg(a), Neg(b))
/// </summary>
public sealed class NegDistributiveRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "neg-distributive";

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
                if (innerNode is Add add)
                {
                    var negLeft = egraph.add(new Neg(add.left)).value;
                    var negRight = egraph.add(new Neg(add.right)).value;
                    yield return (node, new Add(negLeft, negRight));
                }
        }
    }
}