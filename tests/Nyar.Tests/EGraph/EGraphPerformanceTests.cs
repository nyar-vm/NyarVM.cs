using System.Diagnostics;
using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.EGraph;

public class EGraphPerformanceTests
{
    private static EGraph<AlgebraNode> create_e_graph(int capacity = 0)
    {
        return new EGraph<AlgebraNode>(null, null, capacity);
    }

    [Fact]
    public void Performance_AddLargeGraph_10000Nodes()
    {
        var egraph = create_e_graph(10000);
        var sw = Stopwatch.StartNew();

        var constants = new Id[100];
        for (var i = 0; i < 100; i++) constants[i] = egraph.add(new Literal<long>(i));

        for (var i = 0; i < 10000; i++)
        {
            var left = constants[Random.Shared.Next(100)];
            var right = constants[Random.Shared.Next(100)];
            egraph.add(new Add(left, right));
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2000, $"10000 节点 Add 耗时 {sw.ElapsedMilliseconds}ms，超过 2000ms");
        Assert.True(egraph.classes.Count >= 100);
    }

    [Fact]
    public void Performance_UnionLargeGraph_1000Unions()
    {
        var egraph = create_e_graph(2000);
        var sw = Stopwatch.StartNew();

        var nodes = new Id[1000];
        for (var i = 0; i < 1000; i++) nodes[i] = egraph.add(new Literal<long>(i));

        for (var i = 0; i < 500; i++) egraph.union(nodes[i * 2], nodes[i * 2 + 1]);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 1000, $"500 次 Union 耗时 {sw.ElapsedMilliseconds}ms，超过 1000ms");
    }

    [Fact]
    public void Performance_RebuildLargeGraph()
    {
        var egraph = create_e_graph(2000);

        var constants = new Id[100];
        for (var i = 0; i < 100; i++) constants[i] = egraph.add(new Literal<long>(i));

        var addNodes = new Id[500];
        for (var i = 0; i < 500; i++)
        {
            var left = constants[i % 100];
            var right = constants[(i + 1) % 100];
            addNodes[i] = egraph.add(new Add(left, right));
        }

        var mulNodes = new Id[500];
        for (var i = 0; i < 500; i++)
        {
            var left = constants[i % 100];
            var right = constants[(i + 1) % 100];
            mulNodes[i] = egraph.add(new Mul(left, right));
        }

        for (var i = 0; i < 50; i++) egraph.union(addNodes[i], mulNodes[i]);

        var sw = Stopwatch.StartNew();
        egraph.rebuild();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1000, $"Rebuild 耗时 {sw.ElapsedMilliseconds}ms，超过 1000ms");
    }

    [Fact]
    public void Performance_SaturationLargeGraph()
    {
        var egraph = create_e_graph(5000);

        var constants = new Id[50];
        for (var i = 0; i < 50; i++) constants[i] = egraph.add(new Literal<long>(i));

        for (var i = 0; i < 1000; i++)
        {
            var left = constants[Random.Shared.Next(50)];
            var right = constants[Random.Shared.Next(50)];
            egraph.add(new Add(left, right));
        }

        for (var i = 0; i < 500; i++)
        {
            var left = constants[Random.Shared.Next(50)];
            var right = constants[Random.Shared.Next(50)];
            egraph.add(new Mul(left, right));
        }

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);

        var sw = Stopwatch.StartNew();
        var result = engine.run(egraph);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 5000, $"饱和优化耗时 {sw.ElapsedMilliseconds}ms，超过 5000ms");
    }

    [Fact]
    public void Performance_ExtractLargeGraph()
    {
        var egraph = create_e_graph(5000);

        var constants = new Id[50];
        for (var i = 0; i < 50; i++) constants[i] = egraph.add(new Literal<long>(i));

        var roots = new Id[100];
        for (var i = 0; i < 100; i++)
        {
            var a = constants[Random.Shared.Next(50)];
            var b = constants[Random.Shared.Next(50)];
            var add = egraph.add(new Add(a, b));
            var c = constants[Random.Shared.Next(50)];
            roots[i] = egraph.add(new Mul(add, c));
        }

        var dialect = new CoreDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        engine.run(egraph);

        var costModel = new CompositeCostModel(dialect.cost_hooks);
        var extractor = new Extractor(egraph, costModel);

        var sw = Stopwatch.StartNew();
        foreach (var root in roots)
        {
            var tree = extractor.extract(root);
            Assert.NotNull(tree);
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 2000, $"100 次 Extract 耗时 {sw.ElapsedMilliseconds}ms，超过 2000ms");
    }

    [Fact]
    public void Performance_UnionFind_ArrayVsDictionary()
    {
        var uf = new UnionFind(10000);

        var sw = Stopwatch.StartNew();
        for (uint i = 0; i < 10000; i++) uf.register(new Id(i));
        sw.Stop();
        var registerTime = sw.ElapsedMilliseconds;

        sw.Restart();
        for (uint i = 0; i < 5000; i++) uf.union(new Id(i * 2), new Id(i * 2 + 1));
        sw.Stop();
        var unionTime = sw.ElapsedMilliseconds;

        sw.Restart();
        for (uint i = 0; i < 10000; i++) uf.find(new Id(i));
        sw.Stop();
        var findTime = sw.ElapsedMilliseconds;

        Assert.True(registerTime < 100, $"Register 10000 耗时 {registerTime}ms");
        Assert.True(unionTime < 100, $"Union 5000 耗时 {unionTime}ms");
        Assert.True(findTime < 100, $"Find 10000 耗时 {findTime}ms");
    }
}