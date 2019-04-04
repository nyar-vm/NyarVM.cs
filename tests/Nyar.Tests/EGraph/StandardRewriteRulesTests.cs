using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Standard;
using Nyar.Dialect.Standard.Cost;
using Nyar.Dialect.Standard.Nodes;
using Nyar.Dialect.Standard.Rules;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.Tests.EGraph;

/// <summary>
///     Standard 方言等价重写规则测试
/// </summary>
public class StandardRewriteRulesTests
{
    /// <summary>
    ///     创建测试用 EGraph 实例
    /// </summary>
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void StandardDialect_HasCorrectRewriteRulesCount()
    {
        var dialect = new StandardDialect();
        Assert.Equal(11, dialect.rules.Count);
    }

    [Fact]
    public void StandardCostHook_HandlesAllKeyNodes()
    {
        var hook = new StandardCostHook();
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var ptr = egraph.add(new Literal<long>(256));

        Assert.True(hook.CanHandle(new Utf8Concat(a, b)));
        Assert.True(hook.CanHandle(new ArrayNew(a, b)));
        Assert.True(hook.CanHandle(new StructNew("Point", [a])));
        Assert.True(hook.CanHandle(new AtomicCas(ptr, a, b, "seq_cst")));
    }

    [Fact]
    public void StandardCostHook_ReturnsReasonableCosts()
    {
        var hook = new StandardCostHook();
        var egraph = create_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var ptr = egraph.add(new Literal<long>(256));

        var concatCost = hook.Estimate(new Utf8Concat(a, b));
        Assert.True(concatCost.latency > 0, "Utf8Concat 成本应大于 0");

        var atomicCost = hook.Estimate(new AtomicCas(ptr, a, b, "seq_cst"));
        Assert.True(atomicCost.latency > 0, "AtomicCas 成本应大于 0");
    }

    [Fact]
    public void StandardBuiltin_HasCorrectValues()
    {
        Assert.Equal(0x9001L, (long)StandardBuiltin.Cast);
        Assert.Equal(0x9021L, (long)StandardBuiltin.Utf8Concat);
        Assert.Equal(0x9031L, (long)StandardBuiltin.ArrayNew);
        Assert.Equal(0x9041L, (long)StandardBuiltin.StructDeclare);
        Assert.Equal(0x9051L, (long)StandardBuiltin.AtomicCas);
    }
}
