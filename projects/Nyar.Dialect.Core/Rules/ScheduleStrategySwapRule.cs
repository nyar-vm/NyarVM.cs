using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core;

/// <summary>
///     Schedule 与策略交换：Schedule 可穿透 Tile/Vectorize/Unroll
///     Schedule(Gpu, Tile(n, x)) → Tile(n, Schedule(Gpu, x))
///     调度目标应尽可能靠近实际计算
/// </summary>
public sealed class ScheduleStrategySwapRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "schedule-strategy-swap";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not StrategyNode.Schedule sched) continue;

            var innerClass = egraph.get_class(sched.computation);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                var swapped = swap_schedule_into_strategy(egraph, sched.target, innerNode);
                if (swapped is not null) yield return (node, swapped);
            }
        }
    }

    private static AlgebraNode? swap_schedule_into_strategy(EGraph<AlgebraNode> egraph, ExecutionTarget target,
        AlgebraNode strategy)
    {
        var innerSchedule = new StrategyNode.Schedule(target, extract_computation(strategy));
        var innerScheduleId = egraph.add(innerSchedule).value;

        return strategy switch
        {
            StrategyNode.Tile t => new StrategyNode.Tile(t.factor, innerScheduleId),
            StrategyNode.Vectorize v => new StrategyNode.Vectorize(v.width, innerScheduleId),
            StrategyNode.Unroll u => new StrategyNode.Unroll(u.factor, innerScheduleId),
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