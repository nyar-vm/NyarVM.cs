using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Core.Rules;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.EGraph;

public class AlgebraicRuleTests
{
    private static EGraph<IKun> create_e_graph()
    {
        return new EGraph<IKun>(null, IKunNodeComparer.instance);
    }

    [Fact]
    public void AddCommutative_ProducesEquivalentExpression()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Add(a, b));

        var engine = new SaturationEngine<IKun>([new AddCommutativeRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(add);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Count >= 2);

        var hasOriginal = eclass.nodes.Any(n => n is Add addNode && addNode.Left == a && addNode.Right == b);
        var hasSwapped = eclass.nodes.Any(n => n is Add addNode && addNode.Left == b && addNode.Right == a);
        Assert.True(hasOriginal);
        Assert.True(hasSwapped);
    }

    [Fact]
    public void MulCommutative_ProducesEquivalentExpression()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(3));
        var b = egraph.add(new Literal<long>(5));
        var mul = egraph.add(new Mul(a, b));

        var engine = new SaturationEngine<IKun>([new MulCommutativeRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(mul);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Count >= 2);
    }

    [Fact]
    public void AddAssociative_RewritesLeftAssociatedToAddRightAssociated()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var c = egraph.add(new Literal<long>(3));

        var innerAdd = egraph.add(new Add(a, b));
        var outerAdd = egraph.add(new Add(innerAdd, c));

        var engine = new SaturationEngine<IKun>([new AddAssociativeRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
    }

    [Fact]
    public void MulDistributive_ExpandsMulOverAdd()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(2));
        var b = egraph.add(new Literal<long>(3));
        var c = egraph.add(new Literal<long>(4));

        var add = egraph.add(new Add(b, c));
        var mul = egraph.add(new Mul(a, add));

        var engine = new SaturationEngine<IKun>([new MulDistributiveRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
    }

    [Fact]
    public void AllAlgebraicRules_Saturate()
    {
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var c = egraph.add(new Literal<long>(3));

        var add = egraph.add(new Add(a, b));
        var mul = egraph.add(new Mul(c, add));

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<IKun>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.iterations > 0);
        Assert.True(result.is_saturated);
    }
}