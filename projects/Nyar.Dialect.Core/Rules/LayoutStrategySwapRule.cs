using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core;

/// <summary>
///     Layout 与策略交换：Layout 可穿透 Tile/Vectorize/Unroll
///     Layout(SoA, Tile(n, x)) → Tile(n, Layout(SoA, x))
///     布局应尽可能靠近数据源
/// </summary>
public sealed class LayoutStrategySwapRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "layout-strategy-swap";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not StrategyNode.Layout layout) continue;

            var innerClass = egraph.get_class(layout.data);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                var swapped = swap_layout_into_strategy(egraph, layout.kind, innerNode);
                if (swapped is not null) yield return (node, swapped);
            }
        }
    }

    private static AlgebraNode? swap_layout_into_strategy(EGraph<AlgebraNode> egraph, LayoutKind kind,
        AlgebraNode strategy)
    {
        var innerLayout = new StrategyNode.Layout(kind, extract_computation(strategy));
        var innerLayoutId = egraph.add(innerLayout).value;

        return strategy switch
        {
            StrategyNode.Tile t => new StrategyNode.Tile(t.factor, innerLayoutId),
            StrategyNode.Vectorize v => new StrategyNode.Vectorize(v.width, innerLayoutId),
            StrategyNode.Unroll u => new StrategyNode.Unroll(u.factor, innerLayoutId),
            _ => null
        };
    }

    private static Id extract_computation(AlgebraNode strategy)
    {
        return strategy switch
        {
            StrategyNode.Tile t => t.computation,
            StrategyNode.Vectorize v => v.computation,
            StrategyNode.Unroll u => u.computation,
            _ => throw new InvalidOperationException()
        };
    }
}