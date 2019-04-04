using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.IR.Extractor;

/// <summary>
///     Top-K 提取结果，包含从 EGraph 中提取的最优 K 个 Oa 节点
/// </summary>
public sealed class TopKExtractionResult
{
    /// <summary>
    ///     创建 Top-K 提取结果
    /// </summary>
    /// <param name="nodes">K 个程序节点。</param>
    /// <param name="costs">各节点对应的成本。</param>
    public TopKExtractionResult(IReadOnlyList<AlgebraNode> nodes, IReadOnlyList<CostVector> costs)
    {
        this.nodes = nodes;
        this.costs = costs;
    }

    /// <summary>
    ///     提取到的 K 个最优程序节点，按成本升序排列
    /// </summary>
    public IReadOnlyList<AlgebraNode> nodes { get; }


    /// <summary>
    ///     各程序节点对应的成本向量
    /// </summary>
    public IReadOnlyList<CostVector> costs { get; }
}