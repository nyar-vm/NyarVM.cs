using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     映射策略引入规则：Map(f, d) ↔ Tile(n, Vectorize(w, Map(f, d)))
///     为映射操作引入分块+向量化策略供成本模型评估
/// </summary>
public sealed class MapStrategyIntroductionRule : CommonRewriteRule
{
    private readonly int _defaultTileFactor;
    private readonly int _defaultVectorWidth;

    /// <summary>
    ///     创建映射策略引入规则
    /// </summary>
    /// <param name="defaultTileFactor">默认分块因子。</param>
    /// <param name="defaultVectorWidth">默认向量宽度。</param>
    public MapStrategyIntroductionRule(int defaultTileFactor = 64, int defaultVectorWidth = 4)
    {
        _defaultTileFactor = defaultTileFactor;
        _defaultVectorWidth = defaultVectorWidth;
    }

    /// <inheritdoc />
    public override string name => "map-strategy-introduction";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Map map) continue;

            var vectorizedId = egraph.add(new StrategyNode.Vectorize(_defaultVectorWidth, id)).value;
            var tiled = new StrategyNode.Tile(_defaultTileFactor, new Id(vectorizedId));
            yield return (node, tiled);

            var gpuScheduled = new StrategyNode.Schedule(ExecutionTarget.gpu, id);
            yield return (node, gpuScheduled);
        }
    }
}