namespace Nyar.IR.Extractor;

/// <summary>
///     提取策略枚举，控制从 E-Graph 中提取最优程序时使用的算法
/// </summary>
public enum ExtractionStrategy
{
    /// <summary>
    ///     贪心策略，每个等价类直接选择当前成本最低的节点
    /// </summary>
    greedy,

    /// <summary>
    ///     束搜索策略，每个等价类保留前 K 个候选，迭代精炼至收敛
    /// </summary>
    beam_search,

    /// <summary>
    ///     动态规划策略，通过不动点迭代计算全局最优成本
    /// </summary>
    dynamic_programming
}