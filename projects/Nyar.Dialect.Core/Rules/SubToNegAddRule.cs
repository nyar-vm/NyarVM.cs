using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     减法转加法：Sub(a, b) → Add(a, Neg(b))
/// </summary>
public sealed class SubToNegAddRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "sub-to-neg-add";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Sub sub) continue;

            var negRight = egraph.add(new Neg(sub.right)).value;
            yield return (node, new Add(sub.left, negRight));
        }
    }
}