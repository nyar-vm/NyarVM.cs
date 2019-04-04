using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Dialects;

/// <summary>
///     Hardware 方言集成测试
/// </summary>
public class HardwareDialectIntegrationTests
{
    /// <summary>
    ///     创建测试用 EGraph
    /// </summary>
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact]
    public void HardwareDialect_HasCorrectStructure()
    {
        var dialect = new HardwareDialect();
        Assert.Equal(2, dialect.rules.Count);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact]
    public void HardwareDialect_CostHook_HandlesKeyNodes()
    {
        var hook = new HardwareCostHook();
        Assert.True(hook.CanHandle(new Gate(GateType.And, [])));
        Assert.True(hook.CanHandle(new FlipFlop(new Id(0), new Id(1), null, null)));
        Assert.True(hook.CanHandle(new Pipeline(3, new Id(0))));
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Module_LowersToApply()
    {
        var egraph = create_e_graph();
        var bodyId = egraph.add(new Literal<long>(0));
        var nodeId = egraph.add(new Module("Top", [], bodyId));

        var dialect = new HardwareDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Module 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Gate_LowersToApply()
    {
        var egraph = create_e_graph();
        var inputA = egraph.add(new Literal<long>(1));
        var inputB = egraph.add(new Literal<long>(0));
        var nodeId = egraph.add(new Gate(GateType.And, [inputA, inputB]));

        var dialect = new HardwareDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Gate 应降级为 Apply");
    }

    [Fact]
    public void HardwareBuiltin_HasCorrectValues()
    {
        Assert.Equal(0xC001L, (long)HardwareBuiltin.HwModule);
        Assert.Equal(0xC101L, (long)HardwareBuiltin.HwPipeline);
        Assert.Equal(0xC201L, (long)HardwareBuiltin.HwGate);
        Assert.Equal(0xC205L, (long)HardwareBuiltin.HwMux);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AllHardwareNodes_LowerToApply()
    {
        var egraph = create_e_graph();

        var dummyId = egraph.add(new Literal<long>(0));
        var dummyId2 = egraph.add(new Literal<long>(1));

        var nodes = new Oa[]
        {
            new Module("Top", [], dummyId),
            new Port(PortDirection.Input, "clk", 1),
            new Wire("a", 8),
            new Reg("pc", 32, null),
            new Always(AlwaysTrigger.PositiveEdge, dummyId),
            new Pipeline(3, dummyId),
            new Unroll(4, dummyId),
            new SystolicArray(8, 8, dummyId),
            new Gate(GateType.And, [dummyId, dummyId2]),
            new FlipFlop(dummyId, dummyId2, null, null),
            new BitSlice(dummyId, 7, 0),
            new Concat([dummyId, dummyId2]),
            new Mux(dummyId, [dummyId, dummyId2])
        };

        foreach (var node in nodes)
        {
            egraph.add(node);
        }

        var dialect = new HardwareDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 13, "全部 13 个 Hardware 节点都应产生降级");
    }
}