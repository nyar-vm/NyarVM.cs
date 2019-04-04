using Nyar.IR.Intent;
using Nyar.Types;

namespace Nyar.Optimizer.CostModels;

/// <summary>
///     成本模型接口，定义节点成本估算和成本向量比较
/// </summary>
public interface ICostModel
{
    /// <summary>
    ///     计算单个节点的成本向量
    /// </summary>
    /// <param name="node">待评估的 Oa 节点</param>
    /// <returns>该节点的成本向量</returns>
    CostVector node_cost(AlgebraNode node);

    /// <summary>
    ///     比较两个成本向量，返回负数表示 a 优于 b
    /// </summary>
    int compare(CostVector a, CostVector b);
}