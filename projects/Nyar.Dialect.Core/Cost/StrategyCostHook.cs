using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.IR.Strategy;
using Nyar.Optimizer.CostModels;

namespace Nyar.Dialect.Core;

/// <summary>
///     策略节点的成本估算钩子，根据策略类型和参数调整成本
/// </summary>
public sealed class StrategyCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool can_handle(AlgebraNode node)
    {
        return node is StrategyNode.Schedule or StrategyNode.Tile or StrategyNode.Vectorize
            or StrategyNode.Unroll or StrategyNode.Layout or PhysicalNode.Call or PhysicalNode.Access;
    }

    /// <inheritdoc />
    public CostVector estimate(AlgebraNode node)
    {
        return node switch
        {
            StrategyNode.Schedule sched => estimate_schedule(sched),
            StrategyNode.Tile tile => estimate_tile(tile),
            StrategyNode.Vectorize vec => estimate_vectorize(vec),
            StrategyNode.Unroll unroll => estimate_unroll(unroll),
            StrategyNode.Layout layout => estimate_layout(layout),
            PhysicalNode.Call call => estimate_call(call),
            PhysicalNode.Access access => estimate_access(access),
            _ => CostVector.zero
        };
    }

    private static CostVector estimate_schedule(StrategyNode.Schedule sched)
    {
        return sched.target switch
        {
            ExecutionTarget.cpu => CostVector.from_latency(0),
            ExecutionTarget.gpu => new CostVector(100, 5, 0, 0),
            ExecutionTarget.async => new CostVector(10, 1, 0, 0),
            ExecutionTarget.spatial => new CostVector(50, 10, 0, 100),
            ExecutionTarget.comptime => CostVector.from_latency(-100),
            _ => CostVector.zero
        };
    }

    private static CostVector estimate_tile(StrategyNode.Tile tile)
    {
        var overhead = tile.factor > 0 ? 2.0 / tile.factor : 0;
        return CostVector.from_latency(overhead);
    }

    private static CostVector estimate_vectorize(StrategyNode.Vectorize vec)
    {
        var speedup = vec.width > 0 ? -0.5 * vec.width : 0;
        return CostVector.from_latency(speedup);
    }

    private static CostVector estimate_unroll(StrategyNode.Unroll unroll)
    {
        var codeSize = unroll.factor * 4;
        return new CostVector(-0.3 * unroll.factor, 0, codeSize, 0);
    }

    private static CostVector estimate_layout(StrategyNode.Layout layout)
    {
        return layout.kind switch
        {
            LayoutKind.so_a => new CostVector(-2, 0, 0, 0),
            LayoutKind.ao_s => CostVector.zero,
            LayoutKind.auto => CostVector.from_latency(0),
            _ => CostVector.zero
        };
    }

    private static CostVector estimate_call(PhysicalNode.Call call)
    {
        return call.dispatch switch
        {
            DispatchKind.@static => CostVector.from_latency(5),
            DispatchKind.witness => CostVector.from_latency(8),
            DispatchKind.dynamic => CostVector.from_latency(15),
            _ => CostVector.from_latency(10)
        };
    }

    private static CostVector estimate_access(PhysicalNode.Access access)
    {
        return access.dispatch switch
        {
            DispatchKind.@static => CostVector.from_latency(2),
            DispatchKind.witness => CostVector.from_latency(4),
            DispatchKind.dynamic => CostVector.from_latency(8),
            _ => CostVector.from_latency(3)
        };
    }
}