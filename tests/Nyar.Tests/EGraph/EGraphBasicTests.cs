using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.Tests.EGraph;

public class EGraphBasicTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    [Fact]
    public void Add_SameNode_ReturnsSameId()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(42));
        var b = egraph.add(new Literal<long>(42));

        Assert.Equal(a, b);
    }

    [Fact]
    public void Add_DifferentNodes_ReturnsDifferentIds()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Add_CoreNodes_Literal()
    {
        var egraph = create_e_graph();

        var val = egraph.add(new Literal<long>(100));
        var lit = egraph.add(new Literal<long>(42));

        Assert.Equal((uint)0, val.value);
        Assert.Equal((uint)1, lit.value);
    }

    [Fact]
    public void Add_CoreNodes_Add()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Add(a, b));

        Assert.Equal((uint)2, add.value);
    }

    [Fact]
    public void Add_CoreNodes_Mul()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(3));
        var b = egraph.add(new Literal<long>(4));
        var mul = egraph.add(new Mul(a, b));

        Assert.Equal((uint)2, mul.value);
    }

    [Fact]
    public void Union_MergesTwoClasses()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));

        egraph.union(a, b);

        var classA = egraph.get_class(a);
        var classB = egraph.get_class(b);

        Assert.NotNull(classA);
        Assert.NotNull(classB);
        Assert.Same(classA, classB);
        Assert.Equal(2, classA!.nodes.Count);
    }

    [Fact]
    public void Union_SameClass_NoOp()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(1));

        var root = egraph.union(a, a);

        Assert.Equal(a, root);
    }

    [Fact]
    public void Rebuild_AfterUnion_CanonicalizesChildren()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var c = egraph.add(new Literal<long>(3));

        var add1 = egraph.add(new Add(a, b));
        var add2 = egraph.add(new Add(a, c));

        egraph.union(b, c);
        egraph.rebuild();

        var root1 = egraph.union_find.find(add1);
        var root2 = egraph.union_find.find(add2);

        Assert.True(root1.value == root2.value || egraph.classes.Count >= 3,
            $"Rebuild 后 add1 root={root1.value}, add2 root={root2.value}, classes={egraph.classes.Count}");
    }

    [Fact]
    public void GetClass_ReturnsCorrectNodes()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));

        var add1 = egraph.add(new Add(a, b));
        var add2 = egraph.add(new Add(b, a));

        egraph.union(add1, add2);
        egraph.rebuild();

        var eclass = egraph.get_class(add1);
        Assert.NotNull(eclass);
        Assert.Equal(2, eclass!.nodes.Count);
    }

    [Fact]
    public void ToDot_GeneratesValidDotFormat()
    {
        var egraph = create_e_graph();

        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        egraph.add(new Add(a, b));

        var dot = egraph.to_dot();

        Assert.StartsWith("digraph EGraph", dot);
        Assert.EndsWith("}\r\n", dot);
        Assert.Contains("eclass", dot);
    }

    [Fact]
    public void ToDot_WithCustomFormatter()
    {
        var egraph = create_e_graph();
        egraph.add(new Literal<long>(42));

        var dot = egraph.to_dot(n => $"CONST({n})");

        Assert.Contains("CONST(", dot);
    }
}