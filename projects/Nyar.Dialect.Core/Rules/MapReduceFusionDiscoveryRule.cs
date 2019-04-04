using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     映射-归约融合发现规则：Compose(Filter(p, _), Map(f, d)) → Tile(n, Reduce(f, init, Filter(p, d)))
///     当过滤和映射组合时，探索是否可以与后续归约融合
///     这是"融合应为 EGraph 推导结果"理念的具体实现
/// </summary>
public sealed class MapReduceFusionDiscoveryRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "map-reduce-fusion-discovery";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Compose compose) continue;

            var outerClass = egraph.get_class(compose.first);
            var innerClass = egraph.get_class(compose.second);
            if (outerClass is null || innerClass is null) continue;

            foreach (var outerNode in outerClass.nodes)
            {
                if (outerNode is not Reduce reduce) continue;

                foreach (var innerNode in innerClass.nodes)
                {
                    if (innerNode is not Map map) continue;

                    var fused = new Reduce(reduce.function, reduce.initial, map.data);
                    yield return (node, fused);
                }
            }
        }
    }
}