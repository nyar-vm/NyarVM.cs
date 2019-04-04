using Nyar.IR.Intent;
using Nyar.Types;

namespace Nyar.Optimizer.CostModels;

/// <summary>
///     成本模型钩子接口
/// </summary>
public interface ICostModelHook
{
    /// <summary>
    ///     判断此钩子是否能处理指定节点
    /// </summary>
    bool can_handle(AlgebraNode node);

    /// <summary>
    ///     估算节点的成本
    /// </summary>
    CostVector estimate(AlgebraNode node);
}