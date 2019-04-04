using Nyar.Dialect.Schedule.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;
using Task = Nyar.Dialect.Schedule.Nodes.Task;

namespace Nyar.Dialect.Schedule.Cost;

/// <summary>
///     Schedule 方言节点的成本估算钩子
/// </summary>
public sealed class ScheduleCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Task or TaskSet or Precedence or TimeWindow
            or MutualExclusion or CoLocation or SoftConstraint
            or ResourcePool or ResourceAssignment
            or MinimizeMakespan or MinimizeTotalDelay or MinimizeResourceCost
            or BalanceLoad or MultiObjective or ScheduleStrategy or ScheduleResult;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Task => CostVector.from_latency(5),
            TaskSet => CostVector.from_latency(3),
            Precedence => CostVector.from_latency(2),
            TimeWindow => CostVector.from_latency(3),
            MutualExclusion => CostVector.from_latency(2),
            CoLocation => CostVector.from_latency(2),
            SoftConstraint => CostVector.from_latency(1),
            ResourcePool => CostVector.from_latency(5),
            ResourceAssignment => CostVector.from_latency(8),
            MinimizeMakespan => CostVector.from_latency(50),
            MinimizeTotalDelay => CostVector.from_latency(50),
            MinimizeResourceCost => CostVector.from_latency(60),
            BalanceLoad => CostVector.from_latency(40),
            MultiObjective => CostVector.from_latency(30),
            ScheduleStrategy ss => ss.algorithm switch
            {
                ScheduleAlgorithm.FirstFit => CostVector.from_latency(10),
                ScheduleAlgorithm.BestFit => CostVector.from_latency(20),
                ScheduleAlgorithm.CriticalPath => CostVector.from_latency(30),
                ScheduleAlgorithm.ListScheduling => CostVector.from_latency(25),
                ScheduleAlgorithm.GeneticAlgorithm => CostVector.from_latency(500),
                ScheduleAlgorithm.SimulatedAnnealing => CostVector.from_latency(400),
                ScheduleAlgorithm.IntegerLinearProgramming => CostVector.from_latency(1000),
                ScheduleAlgorithm.ConstraintProgramming => CostVector.from_latency(800),
                _ => CostVector.from_latency(100)
            },
            ScheduleResult => CostVector.from_latency(10),
            _ => CostVector.zero
        };
    }
}