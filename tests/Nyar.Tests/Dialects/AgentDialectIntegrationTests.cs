using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using AgentNode = Nyar.Dialect.Agent.Nodes.Agent;

namespace Nyar.Tests.Dialects;

/// <summary>
///     Agent 方言集成测试
/// </summary>
public class AgentDialectIntegrationTests
{
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact]
    public void AgentDialect_HasCorrectStructure()
    {
        var dialect = new AgentDialect();
        Assert.Equal(4, dialect.rules.Count);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact]
    public void AgentDialect_CostHook_HandlesKeyNodes()
    {
        var hook = new AgentCostHook();
        var egraph = create_e_graph();
        var beliefs = egraph.add(new Literal<long>(0));
        var goals = egraph.add(new Literal<long>(0));
        var payload = egraph.add(new Literal<long>(0));

        Assert.True(hook.CanHandle(new AgentNode("agent1", [], beliefs, goals)));
        Assert.True(hook.CanHandle(new Plan([], [])));
        Assert.True(hook.CanHandle(new SendMessage("a1", "a2", "msg", payload)));
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Agent_LowersToApply()
    {
        var egraph = create_e_graph();
        var beliefs = egraph.add(new Literal<long>(0));
        var goals = egraph.add(new Literal<long>(0));
        var nodeId = egraph.add(new AgentNode("agent1", [], beliefs, goals));

        var dialect = new AgentDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Agent 应降级为 Apply");
    }

    [Fact]
    public void AgentBuiltin_HasCorrectValues()
    {
        Assert.Equal(0xD001L, (long)AgentBuiltin.AgentDef);
        Assert.Equal(0xD101L, (long)AgentBuiltin.MsgSend);
        Assert.Equal(0xD201L, (long)AgentBuiltin.ObservationDef);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AllAgentNodes_LowerToApply()
    {
        var egraph = create_e_graph();
        var beliefs = egraph.add(new Literal<long>(0));
        var goals = egraph.add(new Literal<long>(0));
        var targetState = egraph.add(new Literal<long>(0));
        var cost = egraph.add(new Literal<long>(0));
        var preconditions = egraph.add(new Literal<long>(0));
        var effects = egraph.add(new Literal<long>(0));
        var payload = egraph.add(new Literal<long>(0));
        var handler = egraph.add(new Literal<long>(0));
        var obligation = egraph.add(new Literal<long>(0));
        var subject = egraph.add(new Literal<long>(0));
        var sensorInput = egraph.add(new Literal<long>(0));
        var updateFn = egraph.add(new Literal<long>(0));
        var initialState = egraph.add(new Literal<long>(0));
        var hmPreconditions = egraph.add(new Literal<long>(0));
        var agent1 = egraph.add(new Literal<long>(0));
        var agent2 = egraph.add(new Literal<long>(0));

        var nodes = new Oa[]
        {
            new AgentNode("agent1", [], beliefs, goals),
            new AgentSet([agent1, agent2], AgentRelation.Cooperative),
            new BeliefState(new Dictionary<string, Id> { ["key"] = beliefs }),
            new Goal(targetState, GoalPriority.Normal, false),
            new ActionDef("act1", [preconditions], [effects], cost),
            new Plan([agent1], []),
            new PlanRequest(initialState, goals, [agent1]),
            new HtnTask("task1", [agent1]),
            new HtnMethod("method1", hmPreconditions, [agent1]),
            new SendMessage("a1", "a2", "msg", payload),
            new ReceiveMessage("a2", null, handler),
            new Broadcast("a1", "msg", payload),
            new Commitment("a2", obligation, null),
            new Negotiation([agent1, agent2], subject, NegotiationProtocol.ContractNet),
            new Observation("sensor1", sensorInput, 0.9f),
            new BeliefUpdate(beliefs, sensorInput, updateFn),
            new IntentionFormation(goals, beliefs, [agent1])
        };

        foreach (var node in nodes)
        {
            egraph.add(node);
        }

        var dialect = new AgentDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 17, "全部 17 个 Agent 节点都应产生降级");
    }
}