using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using Nyar.IR.Strategy;

namespace Nyar.Dialect.Core;

/// <summary>
///     Schedule 冲突消解：外层 Schedule 覆盖内层
///     Schedule(Gpu, Schedule(Cpu, x)) → Schedule(Gpu, x)
///     外层调度目标优先级更高
/// </summary>
public sealed class ScheduleConflictRule : CommonRewriteRule
{
    /// <inheritdoc />
    public override string name => "schedule-conflict";

    /// <inheritdoc />
    protected override IEnumerable<(AlgebraNode Pattern, AlgebraNode Replacement)> apply_to_class(
        EGraph<AlgebraNode> egraph, Id id)
    {
        var eclass = egraph.get_class(id);
        if (eclass is null) yield break;

        foreach (var node in eclass.nodes)
        {
            if (node is not StrategyNode.Schedule outer) continue;

            var innerClass = egraph.get_class(outer.computation);
            if (innerClass is null) continue;

            foreach (var innerNode in innerClass.nodes)
            {
                if (innerNode is not StrategyNode.Schedule inner) continue;

                yield return (node, new StrategyNode.Schedule(outer.target, inner.computation));
            }
        }
    }
}