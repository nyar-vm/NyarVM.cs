namespace Nyar.Optimizer.CostModels;

/// <summary>
///     多维度成本权重配置，用于将 <see cref="CostVector" /> 的各个维度加权求和，
///     获得单一的综合成本分数，以便在不同优化目标之间进行比较与选择。
/// </summary>
public sealed class CostWeights
{
    /// <summary>
    ///     使用指定的各维度权重构造 <see cref="CostWeights" /> 实例。
    /// </summary>
    /// <param name="latencyWeight">延迟维度的权重，默认 0.4</param>
    /// <param name="powerWeight">功耗维度的权重，默认 0.2</param>
    /// <param name="memoryWeight">内存维度的权重，默认 0.000001</param>
    /// <param name="areaWeight">面积维度的权重，默认 0.000001</param>
    public CostWeights(double latencyWeight = 0.4, double powerWeight = 0.2, double memoryWeight = 0.000001,
        double areaWeight = 0.000001)
    {
        latency_weight = latencyWeight;
        power_weight = powerWeight;
        memory_weight = memoryWeight;
        area_weight = areaWeight;
    }

    /// <summary>
    ///     延迟维度的加权系数。
    /// </summary>
    public double latency_weight { get; }

    /// <summary>
    ///     功耗维度的加权系数。
    /// </summary>
    public double power_weight { get; }

    /// <summary>
    ///     内存维度的加权系数。
    /// </summary>
    public double memory_weight { get; }

    /// <summary>
    ///     硬件面积维度的加权系数。
    /// </summary>
    public double area_weight { get; }

    /// <summary>
    ///     获取默认的成本权重配置，各种维度取均衡的默认值。
    /// </summary>
    public static CostWeights @default => new();

    /// <summary>
    ///     获取以最小化延迟为唯一优化目标的权重配置。
    /// </summary>
    public static CostWeights minimize_latency => new(1.0, 0.0, 0.0, 0.0);

    /// <summary>
    ///     获取以最小化功耗为唯一优化目标的权重配置。
    /// </summary>
    public static CostWeights minimize_power => new(0.0, 1.0, 0.0, 0.0);

    /// <summary>
    ///     获取以最小化内存占用为唯一优化目标的权重配置。
    /// </summary>
    public static CostWeights minimize_memory => new(0.0, 0.0, 1.0, 0.0);

    /// <summary>
    ///     获取以最小化硬件面积为唯一优化目标的权重配置。
    /// </summary>
    public static CostWeights minimize_area => new(0.0, 0.0, 0.0, 1.0);

    /// <summary>
    ///     根据当前权重配置，计算指定成本向量的综合加权分数。
    /// </summary>
    /// <param name="cost">待计算综合分数的成本向量</param>
    /// <returns>各维度成本与对应权重乘积之和</returns>
    public double compute_score(CostVector cost)
    {
        return cost.latency * latency_weight
               + cost.power * power_weight
               + cost.memory * memory_weight
               + cost.area * area_weight;
    }

    /// <summary>
    ///     使用当前权重配置比较两个成本向量的优劣。
    /// </summary>
    /// <param name="a">第一个成本向量</param>
    /// <param name="b">第二个成本向量</param>
    /// <returns>
    ///     若 <paramref name="a" /> 的综合分数更低则返回负数，
    ///     若 <paramref name="b" /> 的综合分数更低则返回正数，
    ///     相等则返回 0。
    /// </returns>
    public int compare(CostVector a, CostVector b)
    {
        return compute_score(a).CompareTo(compute_score(b));
    }
}