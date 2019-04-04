using Nyar.Dialect.Hardware.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.Dialect.Hardware.Cost;

/// <summary>
///     Hardware 方言的成本模型钩子
/// </summary>
public sealed class HardwareCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Gate or FlipFlop or Pipeline;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Gate gate => gate.type switch
            {
                GateType.And or GateType.Or or GateType.Not => new CostVector(1, 1, 0, 1),
                GateType.Nand or GateType.Nor or GateType.Xor or GateType.Xnor => new CostVector(2, 1, 0, 2),
                _ => new CostVector(2, 2, 0, 2)
            },
            FlipFlop => new CostVector(5, 3, 1, 5),
            Pipeline pipeline => new CostVector(
                pipeline.stageCount * 10,
                pipeline.stageCount * 5,
                pipeline.stageCount,
                pipeline.stageCount * 10),
            _ => new CostVector(1, 1, 0, 1)
        };
    }
}