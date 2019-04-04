using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core;

/// <summary>
///     嵌套分块合并：Tile(a, Tile(b, x)) → Tile(a * b, x)
///     两次分块等价于一次更大的分块
/// </summary>
public sealed class TileNestingRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "tile-nesting";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not StrategyNode.Tile outer) continue;

            var innerClass = egraph.get_class(outer.computation);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not StrategyNode.Tile inner) continue;

                yield return (node, new StrategyNode.Tile(outer.factor * inner.factor, inner.computation));
            }
        }
    }
}