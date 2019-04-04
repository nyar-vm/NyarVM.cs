using Nyar.Dialect.Schema.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.Dialect.Schema.Cost;

public sealed class SchemaCostHook : ICostModelHook
{
    public bool CanHandle(AlgebraNode node)
    {
        return node is SchemaModel or SchemaField or SchemaStorage
            or SchemaService or SchemaHttpEndpoint or SchemaGrpcEndpoint;
    }

    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            SchemaModel => CostVector.zero,
            SchemaField => CostVector.zero,
            SchemaStorage => CostVector.zero,
            SchemaService => CostVector.zero,
            SchemaHttpEndpoint => CostVector.zero,
            SchemaGrpcEndpoint => CostVector.zero,
            _ => CostVector.zero
        };
    }
}