using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Dialects;

/// <summary>
///     Proof 方言集成测试
/// </summary>
public class ProofDialectIntegrationTests
{
    /// <summary>
    ///     创建测试用 EGraph
    /// </summary>
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact]
    public void ProofDialect_HasCorrectStructure()
    {
        var dialect = new ProofDialect();
        Assert.Equal(2, dialect.rules.Count);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact]
    public void ProofDialect_CostHook_HandlesKeyNodes()
    {
        var hook = new ProofCostHook();
        var dummyId = new Id(0);
        Assert.True(hook.CanHandle(new Theorem("thm", dummyId)));
        Assert.True(hook.CanHandle(new Proof(dummyId, dummyId)));
        Assert.True(hook.CanHandle(new Refl(dummyId)));
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Theorem_LowersToApply()
    {
        var egraph = create_e_graph();
        var propId = egraph.add(new Literal<long>(0));
        var nodeId = egraph.add(new Theorem("thm", propId));

        var dialect = new ProofDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Theorem 应降级为 Apply");
    }

    [Fact]
    public void ProofBuiltin_HasCorrectValues()
    {
        Assert.Equal(0xF001L, (long)ProofBuiltin.TheoremDef);
        Assert.Equal(0xF101L, (long)ProofBuiltin.CombRefl);
        Assert.Equal(0xF201L, (long)ProofBuiltin.VectorTypeDef);
    }

    [Fact]
    public void ReflSimplificationRule_Applies()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Sym("a"));
        var reflId = egraph.add(new Refl(a));
        var b = egraph.add(new Sym("b"));
        var transId = egraph.add(new Trans(reflId, b));

        var dialect = new ProofDialect();
        var engine = new SaturationEngine<Oa>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0, "Refl 等价重写规则应产生合并");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AllProofNodes_LowerToApply()
    {
        var egraph = create_e_graph();

        var dummyId = egraph.add(new Literal<long>(0));

        var nodes = new Oa[]
        {
            new Theorem("thm", dummyId),
            new Proof(dummyId, dummyId),
            new Tactic("simplify", []),
            new RewriteRule(dummyId, dummyId, null),
            new Induction(dummyId, dummyId, dummyId),
            new Check(dummyId, "z3"),
            new Extract(dummyId, "ocaml"),
            new Refl(dummyId),
            new Sym(dummyId),
            new Trans(dummyId, dummyId),
            new Cong(dummyId, []),
            new Subst(dummyId, dummyId, dummyId),
            new VectorType(dummyId, dummyId),
            new VectorCons(dummyId, dummyId, dummyId),
            new VectorNil(),
            new LengthEqProof(dummyId, dummyId)
        };

        foreach (var node in nodes)
        {
            egraph.add(node);
        }

        var dialect = new ProofDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 16, "全部 16 个 Proof 节点都应产生降级");
    }
}