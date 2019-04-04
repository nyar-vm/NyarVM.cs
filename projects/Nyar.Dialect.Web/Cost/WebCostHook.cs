using Nyar.Dialect.Web.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.Dialect.Web.Cost;

/// <summary>
///     Web 方言节点的成本估算钩�?///
/// </summary>
public sealed class WebCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Element or TextNode or Fragment or Component or Attr or Style
            or Event or Cond or ListRender or Route or Middleware or Request or Response
            or Redirect or Json or Guard or DomQuery or DomMutate or StorageGet
            or StorageSet;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Element => CostVector.from_latency(5),
            TextNode => CostVector.from_latency(1),
            Fragment => CostVector.from_latency(0.5),
            Component => CostVector.from_latency(10),
            Attr => CostVector.from_latency(1),
            Style => CostVector.from_latency(2),
            Event => CostVector.from_latency(5),
            Cond => CostVector.from_latency(2),
            ListRender => CostVector.from_latency(10),
            Route => CostVector.from_latency(5),
            Middleware => CostVector.from_latency(3),
            Request => CostVector.from_latency(1),
            Response => CostVector.from_latency(2),
            Redirect => CostVector.from_latency(1),
            Json => CostVector.from_latency(10),
            Guard => CostVector.from_latency(2),
            DomQuery => CostVector.from_latency(5),
            DomMutate => CostVector.from_latency(5),
            StorageGet => CostVector.from_latency(3),
            StorageSet => CostVector.from_latency(3),
            _ => CostVector.zero
        };
    }
}