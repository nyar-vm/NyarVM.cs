using Nyar.Dialect.Schedule.Nodes;
using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Schedule;

/// <summary>
///     Schedule 方言的 OA 接口定义。
///     该接口声明任务、资源、约束、目标与调度策略相关操作。
/// </summary>
[Dialect("schedule")]
public interface ISchedule<T>
{
    /// <summary>
    ///     定义任务。
    /// </summary>
    [Operator("task")]
    Term<T> Task(string taskId, Term<T> duration, IReadOnlyList<ResourceRequirement> resources, int priority);

    /// <summary>
    ///     聚合多个任务。
    /// </summary>
    [Operator("task_set")]
    Term<T> TaskSet(Term<T> tasks);

    /// <summary>
    ///     定义前序约束。
    /// </summary>
    [Operator("precedence")]
    Term<T> Precedence(Term<T> beforeTask, Term<T> afterTask);

    /// <summary>
    ///     定义时间窗约束。
    /// </summary>
    [Operator("time_window")]
    Term<T> TimeWindow(Term<T> task, Term<T> earliestStart, Term<T> latestFinish);

    /// <summary>
    ///     定义互斥约束。
    /// </summary>
    [Operator("mutual_exclusion")]
    Term<T> MutualExclusion(Term<T> taskA, Term<T> taskB);

    /// <summary>
    ///     定义共置约束。
    /// </summary>
    [Operator("co_location")]
    Term<T> CoLocation(Term<T> taskA, Term<T> taskB);

    /// <summary>
    ///     定义软约束。
    /// </summary>
    [Operator("soft_constraint")]
    Term<T> SoftConstraint(Term<T> constraint, float penaltyWeight);

    /// <summary>
    ///     定义资源池。
    /// </summary>
    [Operator("resource_pool")]
    Term<T> ResourcePool(
        string poolId,
        string resourceType,
        int capacity,
        IReadOnlyList<ResourceAttribute> attributes);

    /// <summary>
    ///     分配资源。
    /// </summary>
    [Operator("resource_assignment")]
    Term<T> ResourceAssignment(Term<T> task, string poolId, int allocatedAmount);

    /// <summary>
    ///     目标为最小化工期。
    /// </summary>
    [Operator("minimize_makespan")]
    Term<T> MinimizeMakespan(Term<T> scheduleRef);

    /// <summary>
    ///     目标为最小化总延迟。
    /// </summary>
    [Operator("minimize_total_delay")]
    Term<T> MinimizeTotalDelay(Term<T> scheduleRef);

    /// <summary>
    ///     目标为最小化资源成本。
    /// </summary>
    [Operator("minimize_resource_cost")]
    Term<T> MinimizeResourceCost(Term<T> scheduleRef, IReadOnlyDictionary<string, float> unitCosts);

    /// <summary>
    ///     目标为平衡负载。
    /// </summary>
    [Operator("balance_load")]
    Term<T> BalanceLoad(Term<T> scheduleRef);

    /// <summary>
    ///     组合多个优化目标。
    /// </summary>
    [Operator("multi_objective")]
    Term<T> MultiObjective(Term<T> objectives, IReadOnlyList<float> weights);

    /// <summary>
    ///     指定调度策略。
    /// </summary>
    [Operator("schedule_strategy")]
    Term<T> ScheduleStrategy(ScheduleAlgorithm algorithm, Term<T> problem);

    /// <summary>
    ///     表示调度结果。
    /// </summary>
    [Operator("schedule_result")]
    Term<T> ScheduleResult(IReadOnlyDictionary<string, TimeSlot> assignments);
}