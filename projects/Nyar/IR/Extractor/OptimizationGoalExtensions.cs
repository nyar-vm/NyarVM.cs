using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.IR.Extractor;

/// <summary>
///     优化目标扩展方法
/// </summary>
public static class OptimizationGoalExtensions
{
    /// <summary>
    ///     比较两个成本向量
    /// </summary>
    public static int compare(this OptimizationGoal goal, CostVector a, CostVector b)
    {
        return goal.to_cost_weights().compare(a, b);
    }

    /// <summary>
    ///     转换为成本权重
    /// </summary>
    public static CostWeights to_cost_weights(this OptimizationGoal goal)
    {
        return goal switch
        {
            OptimizationGoal.minimize_latency => CostWeights.minimize_latency,
            OptimizationGoal.minimize_throughput => CostWeights.minimize_latency,
            OptimizationGoal.minimize_memory => CostWeights.minimize_memory,
            OptimizationGoal.minimize_power => CostWeights.minimize_power,
            OptimizationGoal.minimize_area => CostWeights.minimize_area,
            OptimizationGoal.balanced => CostWeights.@default,
            _ => CostWeights.@default
        };
    }
}