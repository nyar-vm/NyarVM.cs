using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     过滤策略引入规则：Filter(p, d) ↔ Tile(64, Filter(p, d))
///     过滤操作可受益于分块处理（改善缓存局部性）
/// </summary>
public sealed class FilterStrategyIntroductionRule : CommonRewriteRule
{
    private readonly int _defaultTileFactor;

    /// <summary>
    ///     创建过滤策略引入规则
    /// </summary>
    /// <param name="defaultTileFactor">默认分块因子。</param>
    public FilterStrategyIntroductionRule(int defaultTileFactor = 64)
    {
        _defaultTileFactor = defaultTileFactor;
    }

    /// <inheritdoc />
    public override string name => "filter-strategy-introduction";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Filter) continue;

            var tiled = new StrategyNode.Tile(_defaultTileFactor, id);
            yield return (node, tiled);
        }
    }
}