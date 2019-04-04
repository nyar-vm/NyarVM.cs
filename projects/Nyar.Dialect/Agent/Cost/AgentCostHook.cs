using Nyar.Dialect.Agent.Nodes;
using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;

namespace Nyar.Dialect.Agent.Cost;

/// <summary>
///     Agent 方言节点的成本估算钩子
/// </summary>
public sealed class AgentCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is Nodes.Agent or AgentSet or BeliefState or Goal
            or ActionDef or Plan or PlanRequest or HtnTask or HtnMethod
            or SendMessage or ReceiveMessage or Broadcast or Commitment or Negotiation
            or Observation or BeliefUpdate or IntentionFormation;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            Nodes.Agent => CostVector.from_latency(10),
            AgentSet set => new CostVector(set.agents.Count * 8, 0, 0, 0),
            BeliefState bs => new CostVector(bs.facts.Count * 5, 0, 0, 0),
            Goal => CostVector.from_latency(3),
            ActionDef => CostVector.from_latency(8),
            Plan plan => new CostVector(plan.actions.Count * 15, 0, 0, 0),
            PlanRequest => CostVector.from_latency(100),
            HtnTask htn => new CostVector(htn.methods.Count * 20, 0, 0, 0),
            HtnMethod => CostVector.from_latency(15),
            SendMessage => CostVector.from_latency(25),
            ReceiveMessage => CostVector.from_latency(20),
            Broadcast => CostVector.from_latency(30),
            Commitment => CostVector.from_latency(10),
            Negotiation => CostVector.from_latency(200),
            Observation => CostVector.from_latency(15),
            BeliefUpdate => CostVector.from_latency(30),
            IntentionFormation => CostVector.from_latency(80),
            _ => CostVector.zero
        };
    }
}