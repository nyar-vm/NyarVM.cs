using Nyar.Dialect.Core.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;
using Tuple = Nyar.Dialect.Core.Nodes.Tuple;

namespace Nyar.Dialect.Core.Cost;

/// <summary>
///     Core 方言节点的成本估算钩子
/// </summary>
public sealed partial class CoreCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Literal<long> or Literal<double> or Literal<bool>
            or Literal<string> or Literal<object?> or Add or Sub or Mul or Div or Rem or Neg or Not or Cmp
            or Tuple or Project or Alloc or Free or Load or Store or Branch
            or Phi or Call or Ret or Perform or Handle;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Literal<long> or Literal<double> or Literal<bool> or Literal<string> or Literal<object?> => CostVector.zero,
            Add or Sub => CostVector.from_latency(1),
            Mul => CostVector.from_latency(3),
            Div or Rem => CostVector.from_latency(10),
            Neg or Not => CostVector.from_latency(1),
            Cmp => CostVector.from_latency(1),
            Tuple => CostVector.from_latency(1),
            Project => CostVector.from_latency(0.5),
            Alloc => new CostVector(10, 0, 16, 0),
            Free => CostVector.from_latency(1),
            Load => CostVector.from_latency(5),
            Store => CostVector.from_latency(5),
            Branch => CostVector.from_latency(1),
            Phi => CostVector.from_latency(0.5),
            Call => CostVector.from_latency(10),
            Ret => CostVector.from_latency(1),
            Perform => CostVector.from_latency(50),
            Handle => CostVector.from_latency(5),
            _ => CostVector.zero
        };
    }
}