using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core;

/// <summary>
///     嵌套展开合并：Unroll(a, Unroll(b, x)) → Unroll(a * b, x)
/// </summary>
public sealed class UnrollNestingRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "unroll-nesting";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not StrategyNode.Unroll outer) continue;

            var innerClass = egraph.get_class(outer.computation);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not StrategyNode.Unroll inner) continue;

                yield return (node, new StrategyNode.Unroll(outer.factor * inner.factor, inner.computation));
            }
        }
    }
}