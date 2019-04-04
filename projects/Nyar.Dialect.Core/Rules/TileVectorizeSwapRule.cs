using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core;

/// <summary>
///     策略交换律：Tile 和 Vectorize 可交换顺序
///     Tile(n, Vectorize(w, x)) ↔ Vectorize(w, Tile(n, x))
///     这使得 EGraph 可以探索不同的策略应用顺序
/// </summary>
public sealed class TileVectorizeSwapRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "tile-vectorize-swap";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not StrategyNode.Tile tile) continue;

            var innerClass = egraph.get_class(tile.computation);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not StrategyNode.Vectorize vec) continue;

                yield return (node, new StrategyNode.Vectorize(vec.width,
                    egraph.add(new StrategyNode.Tile(tile.factor, vec.computation)).value));
            }
        }
    }
}