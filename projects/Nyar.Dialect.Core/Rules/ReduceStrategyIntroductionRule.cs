using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core.Rules;

/// <summary>
///     归约策略引入规则：Reduce(f, init, d) ↔ Schedule(Gpu, Reduce(f, init, d))
///     归约操作天然适合 GPU 并行，引入 GPU 调度策略供成本模型评估
/// </summary>
public sealed class ReduceStrategyIntroductionRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "reduce-strategy-introduction";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not Reduce) continue;

            var gpuScheduled = new StrategyNode.Schedule(ExecutionTarget.gpu, id);
            yield return (node, gpuScheduled);

            var tiled = new StrategyNode.Tile(256, id);
            yield return (node, tiled);
        }
    }
}