using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.Dialect.Data.Cost;

/// <summary>
///     Data 方言节点的成本估算钩子
/// </summary>
public sealed class DataCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Scan or IndexScan or Filter or Project or Join or Aggregate
            or GroupBy or Having or WindowFunction or Distinct or Subquery or Union
            or OrderBy or Limit or TableStats;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Scan => new CostVector(1000, 0, 0, 0),
            IndexScan => new CostVector(100, 0, 0, 0),
            Filter => CostVector.from_latency(50),
            Project => CostVector.from_latency(10),
            Join => new CostVector(500, 0, 0, 0),
            Aggregate => new CostVector(200, 0, 0, 0),
            GroupBy => new CostVector(300, 0, 0, 0),
            Having => CostVector.from_latency(30),
            WindowFunction => new CostVector(400, 0, 0, 0),
            Distinct => new CostVector(150, 0, 0, 0),
            Subquery => new CostVector(600, 0, 0, 0),
            Union => new CostVector(100, 0, 0, 0),
            OrderBy => new CostVector(300, 0, 0, 0),
            Limit => CostVector.from_latency(10),
            TableStats => CostVector.zero,
            _ => CostVector.zero
        };
    }
}