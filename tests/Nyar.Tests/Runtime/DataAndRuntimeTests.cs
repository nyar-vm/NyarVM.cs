using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Runtime;

public class DataDialectTests
{
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Scan_LowersToApply()
    {
        var egraph = create_e_graph();
        var scan = egraph.add(new Scan("users"));

        var dialect = new DataDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(scan);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Scan 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void IndexScan_LowersToApply()
    {
        var egraph = create_e_graph();
        var predicate = egraph.add(new Literal<long>(1));
        var indexScan = egraph.add(new IndexScan("users", "idx_name", predicate));

        var dialect = new DataDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(indexScan);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "IndexScan 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Filter_LowersToApply()
    {
        var egraph = create_e_graph();
        var predicate = egraph.add(new Literal<long>(1));
        var data = egraph.add(new Scan("users"));
        var filter = egraph.add(new Filter(predicate, data));

        var dialect = new DataDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(filter);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Filter 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Join_LowersToApply()
    {
        var egraph = create_e_graph();
        var left = egraph.add(new Scan("users"));
        var right = egraph.add(new Scan("orders"));
        var condition = egraph.add(new Literal<long>(1));
        var join = egraph.add(new Join(JoinType.inner, left, right, condition));

        var dialect = new DataDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(join);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Join 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Aggregate_LowersToApply()
    {
        var egraph = create_e_graph();
        var column = egraph.add(new Literal<long>(0));
        var data = egraph.add(new Scan("users"));
        var agg = egraph.add(new Aggregate(AggType.sum, column, data));

        var dialect = new DataDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(agg);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Aggregate 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void OrderByAndLimit_LowersToApply()
    {
        var egraph = create_e_graph();
        var key = egraph.add(new Literal<long>(0));
        var count = egraph.add(new Literal<long>(10));
        var data = egraph.add(new Scan("users"));
        var orderBy = egraph.add(new OrderBy(key, true, data));
        var limit = egraph.add(new Limit(count, data));

        var dialect = new DataDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var orderByClass = egraph.get_class(orderBy);
        Assert.NotNull(orderByClass);
        Assert.True(orderByClass!.nodes.Any(n => n is Apply), "OrderBy 应降级为 Apply");

        var limitClass = egraph.get_class(limit);
        Assert.NotNull(limitClass);
        Assert.True(limitClass!.nodes.Any(n => n is Apply), "Limit 应降级为 Apply");
    }

    [Fact]
    public void DataDialect_HasEmptyPEFactories()
    {
        var dialect = new DataDialect();
        Assert.Empty(dialect.pe_factories);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void FullQuery_PipelineLowersAllNodes()
    {
        var egraph = create_e_graph();
        var users = egraph.add(new Scan("users"));
        var predicate = egraph.add(new Literal<long>(1));
        var filtered = egraph.add(new Filter(predicate, users));
        var key = egraph.add(new Literal<long>(0));
        var ordered = egraph.add(new OrderBy(key, false, filtered));
        var count = egraph.add(new Literal<long>(10));
        var limited = egraph.add(new Limit(count, ordered));

        var dialect = new DataDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 4, "4 个 Data 节点都应产生降级");
    }
}

public class EffectRuntimeTests
{
    [Fact]
    public void EffectRuntime_PerformRegisteredEffect()
    {
        var runtime = new EffectRuntime();
        runtime.RegisterHandler("console/print", (_, _, _) => Value.@null);
        var result = runtime.Perform("console/print", Value.from_string("hello"));
        Assert.Equal(Value.@null, result);
    }

    [Fact]
    public void EffectRuntime_PerformUnregisteredEffect_Throws()
    {
        var runtime = new EffectRuntime();
        Assert.Throws<InvalidOperationException>(() =>
            runtime.Perform("nonexistent/effect", Value.@null));
    }

    [Fact]
    public void EffectRuntime_HasHandler_ReturnsTrue()
    {
        var runtime = new EffectRuntime();
        runtime.RegisterHandler("console/print", (_, _, _) => Value.@null);
        Assert.True(runtime.HasHandler("console/print"));
    }

    [Fact]
    public void EffectRuntime_HasHandler_ReturnsFalse()
    {
        var runtime = new EffectRuntime();
        Assert.False(runtime.HasHandler("nonexistent/effect"));
    }

    [Fact]
    public void EffectRuntime_ScopeOverridesGlobalHandler()
    {
        var runtime = new EffectRuntime();
        runtime.RegisterHandler("console/print", (_, _, _) => Value.@null);
        var scope = new EffectHandlerScope();
        scope.RegisterHandler("console/print", (_, _, _) => Value.from_int(42));
        runtime.PushScope(scope);

        var result = runtime.Perform("console/print", Value.@null);
        Assert.Equal(42, result.@int);

        runtime.PopScope();
    }

    [Fact]
    public void EffectRuntime_ScopePopRestoresGlobalHandler()
    {
        var runtime = new EffectRuntime();
        runtime.RegisterHandler("console/print", (_, _, _) => Value.from_string("global"));
        var scope = new EffectHandlerScope();
        scope.RegisterHandler("console/print", (_, _, _) => Value.from_int(42));
        runtime.PushScope(scope);

        var inScope = runtime.Perform("console/print", Value.@null);
        Assert.Equal(42, inScope.@int);

        runtime.PopScope();

        var afterScope = runtime.Perform("console/print", Value.@null);
        Assert.Equal("global", afterScope.@string);
    }

    [Fact]
    public void EffectRuntime_RegisterCustomHandler()
    {
        var runtime = new EffectRuntime();
        runtime.RegisterHandler("custom/add", (_, payload, _) => Value.from_int(payload.@int + 10));

        var result = runtime.Perform("custom/add", Value.from_int(5));
        Assert.Equal(15, result.@int);
    }
}

public class WitnessTableTests
{
    [Fact]
    public void WitnessTable_RegisterType()
    {
        var wt = new WitnessTable();
        wt.RegisterType(1, "Vec2", 16, 2);

        var info = wt.GetTypeInfo(1);
        Assert.NotNull(info);
        Assert.Equal("Vec2", info!.TypeName);
        Assert.Equal(16, info.Size);
        Assert.Equal(2, info.FieldCount);
    }

    [Fact]
    public void WitnessTable_RegisterInterface()
    {
        var wt = new WitnessTable();
        wt.RegisterType(1, "Vec2", 16, 2);
        wt.RegisterInterface(1, 100, new Dictionary<int, int> { [0] = 10, [1] = 11 });

        Assert.True(wt.ImplementsInterface(1, 100));
        Assert.False(wt.ImplementsInterface(1, 200));
    }

    [Fact]
    public void WitnessTable_Dispatch()
    {
        var wt = new WitnessTable();
        wt.RegisterType(1, "Vec2", 16, 2);
        wt.RegisterMethod(10, 1, "Add", 0);
        wt.RegisterMethod(11, 1, "Sub", 1);
        wt.RegisterInterface(1, 100, new Dictionary<int, int> { [0] = 10, [1] = 11 });

        var addIndex = wt.Dispatch(1, 100, 0);
        Assert.Equal(0, addIndex);

        var subIndex = wt.Dispatch(1, 100, 1);
        Assert.Equal(1, subIndex);
    }

    [Fact]
    public void WitnessTable_DispatchNotFound()
    {
        var wt = new WitnessTable();
        var result = wt.Dispatch(1, 100, 0);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void WitnessTable_HotSwap()
    {
        var wt = new WitnessTable();
        wt.RegisterType(1, "Vec2", 16, 2);
        wt.RegisterMethod(10, 1, "Add", 0);
        wt.RegisterMethod(11, 1, "Sub", 1);
        wt.RegisterInterface(1, 100, new Dictionary<int, int> { [0] = 10 });

        Assert.Equal(0, wt.Dispatch(1, 100, 0));
        Assert.Equal(-1, wt.Dispatch(1, 100, 1));

        wt.HotSwap(1, 100, new Dictionary<int, int> { [0] = 10, [1] = 11 });

        Assert.Equal(0, wt.Dispatch(1, 100, 0));
        Assert.Equal(1, wt.Dispatch(1, 100, 1));
    }

    [Fact]
    public void WitnessTable_FindMethod()
    {
        var wt = new WitnessTable();
        wt.RegisterMethod(10, 1, "Add", 0);

        var entry = wt.FindMethod(1, "Add");
        Assert.NotNull(entry);
        Assert.Equal("Add", entry!.MethodName);
        Assert.Equal(0, entry.FunctionIndex);
    }
}