using Nyar.Dialect.Neural.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Neural.Rules;

/// <summary>
///     连续转置消除：Transpose(Transpose(x, p1), p2) -> Transpose(x, compose(p1, p2))
/// </summary>
public sealed class TransposeEliminationRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "transpose-elimination";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Transpose outer) continue;

            var innerClass = egraph.get_class(outer.input);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not Transpose inner) continue;

                var composed = new int[outer.perm.Count];
                for (var i = 0; i < outer.perm.Count; i++) composed[i] = outer.perm[inner.perm[i]];

                yield return (node, new Transpose(inner.input, composed));
            }
        }
    }
}