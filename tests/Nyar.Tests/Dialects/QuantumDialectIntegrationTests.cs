using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Dialects;

/// <summary>
///     Quantum 方言集成测试
/// </summary>
public class QuantumDialectIntegrationTests
{
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact]
    public void QuantumDialect_HasCorrectStructure()
    {
        var dialect = new QuantumDialect();
        Assert.Equal(4, dialect.rules.Count);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact]
    public void QuantumDialect_CostHook_HandlesKeyNodes()
    {
        var hook = new QuantumCostHook();
        var egraph = create_e_graph();
        var target = egraph.add(new Literal<long>(0));
        var qubit = egraph.add(new Literal<long>(0));
        var classicalBit = egraph.add(new Literal<long>(0));

        Assert.True(hook.CanHandle(new Qubit("q0")));
        Assert.True(hook.CanHandle(new SingleQubitGate(SingleGateType.H, target)));
        Assert.True(hook.CanHandle(new Measure(qubit, classicalBit)));
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Qubit_LowersToApply()
    {
        var egraph = create_e_graph();
        var nodeId = egraph.add(new Qubit("q0"));

        var dialect = new QuantumDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Qubit 应降级为 Apply");
    }

    [Fact]
    public void QuantumBuiltin_HasCorrectValues()
    {
        Assert.Equal(0xB001L, (long)QuantumBuiltin.QubitAlloc);
        Assert.Equal(0xB101L, (long)QuantumBuiltin.Gate1Q);
        Assert.Equal(0xB301L, (long)QuantumBuiltin.Circuit);
        Assert.Equal(0xB401L, (long)QuantumBuiltin.AlgorithmQft);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AllQuantumNodes_LowerToApply()
    {
        var egraph = create_e_graph();
        var q0 = egraph.add(new Literal<long>(0));
        var q1 = egraph.add(new Literal<long>(1));
        var q2 = egraph.add(new Literal<long>(2));
        var angle = egraph.add(new Literal<long>(314));
        var cond = egraph.add(new Literal<long>(0));
        var op = egraph.add(new Literal<long>(0));
        var unitary = egraph.add(new Literal<long>(0));
        var hamiltonian = egraph.add(new Literal<long>(0));
        var optimizer = egraph.add(new Literal<long>(0));
        var circuit = egraph.add(new Literal<long>(0));

        var nodes = new Oa[]
        {
            new Qubit("q0"),
            new QuantumRegister("q", 4),
            new ClassicalRegister("c", 4),
            new SingleQubitGate(SingleGateType.H, q0),
            new ParametricSingleGate(SingleGateType.Rz, q0, angle),
            new ControlledGate(ControlledGateType.CX, q0, q1),
            new MultiControlledGate(ControlledGateType.CX, [q0, q1], q2),
            new SwapGate(q0, q1),
            new ToffoliGate(q0, q1, q2),
            new Measure(q0, cond),
            new Reset(q0),
            new ConditionalOperation(cond, op),
            new QuantumCircuit([q0, q1]),
            new CircuitModule("mod", ["q0", "q1"], [q0, q1]),
            new ModuleInstance("mod", new Dictionary<string, Id> { ["q0"] = q0 }),
            new CircuitDepth(circuit),
            new QubitConnectivity([("q0", "q1")]),
            new QuantumFourierTransform([q0, q1]),
            new GroverDiffusion([q0, q1]),
            new PhaseEstimation(unitary, [q0], [q1]),
            new VariationalQuantumEigensolver(unitary, hamiltonian, optimizer)
        };

        foreach (var node in nodes)
        {
            egraph.add(node);
        }

        var dialect = new QuantumDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 21, "全部 21 个 Quantum 节点都应产生降级");
    }
}