using Nyar.IR.Intent;

namespace Nyar.Dialect.Schedule.Nodes;

#region 任务定义节点

[AlgebraNode]
public sealed partial record Task(
    string taskId,
    Id duration,
    IReadOnlyList<ResourceRequirement> resources,
    int priority) : AlgebraNode;

public sealed record ResourceRequirement(string ResourceType, int Amount, ResourceConstraint Constraint);

public enum ResourceConstraint
{
    Exact,
    AtLeast,
    AtMost,
    Range
}

[AlgebraNode]
public sealed partial record TaskSet(Id tasks) : AlgebraNode;

#endregion

#region 调度约束节点

[AlgebraNode]
public sealed partial record Precedence(Id beforeTask, Id afterTask) : AlgebraNode;

[AlgebraNode]
public sealed partial record TimeWindow(Id task, Id earliestStart, Id latestFinish) : AlgebraNode;

[AlgebraNode]
public sealed partial record MutualExclusion(Id taskA, Id taskB) : AlgebraNode;

[AlgebraNode]
public sealed partial record CoLocation(Id taskA, Id taskB) : AlgebraNode;

[AlgebraNode]
public sealed partial record SoftConstraint(Id constraint, float penaltyWeight) : AlgebraNode;

#endregion

#region 资源节点

[AlgebraNode]
public sealed partial record ResourcePool(
    string poolId,
    string resourceType,
    int capacity,
    IReadOnlyList<ResourceAttribute> attributes) : AlgebraNode;

public sealed record ResourceAttribute(string Name, object Value);

[AlgebraNode]
public sealed partial record ResourceAssignment(Id task, string poolId, int allocatedAmount) : AlgebraNode;

#endregion

#region 调度目标节点

[AlgebraNode]
public sealed partial record MinimizeMakespan(Id scheduleRef) : AlgebraNode;

[AlgebraNode]
public sealed partial record MinimizeTotalDelay(Id scheduleRef) : AlgebraNode;

[AlgebraNode]
public sealed partial record MinimizeResourceCost(Id scheduleRef, IReadOnlyDictionary<string, float> unitCosts) : AlgebraNode;

[AlgebraNode]
public sealed partial record BalanceLoad(Id scheduleRef) : AlgebraNode;

[AlgebraNode]
public sealed partial record MultiObjective(Id objectives, IReadOnlyList<float> weights) : AlgebraNode;

#endregion

#region 调度策略节点

[AlgebraNode]
public sealed partial record ScheduleStrategy(ScheduleAlgorithm algorithm, Id problem) : AlgebraNode;

public enum ScheduleAlgorithm
{
    FirstFit,
    BestFit,
    CriticalPath,
    ListScheduling,
    GeneticAlgorithm,
    SimulatedAnnealing,
    IntegerLinearProgramming,
    ConstraintProgramming
}

[AlgebraNode]
public sealed partial record ScheduleResult(IReadOnlyDictionary<string, TimeSlot> assignments) : AlgebraNode;

public sealed record TimeSlot(long StartTime, long EndTime, string AssignedPool);

#endregion