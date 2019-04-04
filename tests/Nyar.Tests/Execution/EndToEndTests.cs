using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Cost;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Execution;

public class EndToEndTests
{
    private static EGraph<IKun> create_e_graph()
    {
        return new EGraph<IKun>(null, IKunNodeComparer.instance);
    }

    [Fact]
    public void Extractor_SelectsCheapestNode()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Add(a, b));

        egraph.union(add, egraph.add(new Add(b, a)));
        egraph.rebuild();

        var costModel = new CompositeCostModel([new CoreCostHook()]);
        var extractor = new Extractor(egraph, costModel);
        var result = extractor.extract(add);

        Assert.NotNull(result);
    }

    [Fact]
    public void FullPipeline_SaturateAndExtract()
    {
        var egraph = create_e_graph();
        var x = egraph.add(new Literal<long>(1));
        var y = egraph.add(new Literal<long>(2));
        var z = egraph.add(new Literal<long>(3));

        var add = egraph.add(new Add(x, y));
        var mul = egraph.add(new Mul(z, add));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<IKun>(dialect.Rules);
        var result = engine.run(egraph);

        Assert.True(result.is_saturated);

        var costModel = new CompositeCostModel(dialect.CostHooks);
        var extractor = new Extractor(egraph, costModel);
        var extracted = extractor.extract(mul);

        Assert.NotNull(extracted);
    }

    [Fact]
    public void Lowering_BinaryOpToLowerLevel()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var binaryOp = egraph.add(new Add(a, b));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<IKun>(dialect.LoweringRules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(binaryOp);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Count >= 2);

        var hasAdd = eclass.nodes.Any(n => n is Add);
        Assert.True(hasAdd);
    }

    [Fact]
    public void Lowering_ChoiceToBranch()
    {
        var egraph = create_e_graph();
        var cond = egraph.add(new Literal<long>(1));
        var then = egraph.add(new Literal<long>(2));
        var @else = egraph.add(new Literal<long>(3));
        var choice = egraph.add(new Choice(cond, then, @else));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<IKun>(dialect.LoweringRules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(choice);
        Assert.NotNull(eclass);
        var hasBranch = eclass!.nodes.Any(n => n is Branch);
        Assert.True(hasBranch);
    }

    [Fact]
    public void Lowering_AllRules_ProduceCoreNodes()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));

        var nodes = new IKun[]
        {
            new Mul(a, b),
            new Neg(a),
            new Apply(a, new Id[] { b }),
            new Ret(a),
            new Choice(a, a, b),
            new StateUp(a, b)
        };

        foreach (var node in nodes)
        {
            egraph.add(node);
        }

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<IKun>(dialect.LoweringRules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
    }
}