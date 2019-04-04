using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.Tests.Runtime;

/// <summary>
///     Data 方言 CostHook 测试
/// </summary>
public class DataCostHookTests
{
    /// <summary>
    ///     创建测试用 EGraph 实例
    /// </summary>
    private static EGraph<IKun> create_e_graph()
    {
        return new EGraph<IKun>(null, IKunNodeComparer.instance);
    }

    [Fact]
    public void DataCostHook_HandlesAllKeyNodes()
    {
        var hook = new DataCostHook();
        var egraph = create_e_graph();
        var pred = egraph.add(new Literal<long>(0));
        var source = egraph.add(new Literal<long>(1));
        var left = egraph.add(new Literal<long>(2));
        var right = egraph.add(new Literal<long>(3));
        var cond = egraph.add(new Literal<long>(4));
        var column = egraph.add(new Literal<long>(5));
        var count = egraph.add(new Literal<long>(10));
        var key = egraph.add(new Literal<long>(6));

        Assert.True(hook.CanHandle(new Scan("users")));
        Assert.True(hook.CanHandle(new IndexScan("users", "idx_name", pred)));
        Assert.True(hook.CanHandle(new Filter(pred, source)));
        Assert.True(hook.CanHandle(new Project([], source)));
        Assert.True(hook.CanHandle(new Join(JoinType.Inner, left, right, cond)));
        Assert.True(hook.CanHandle(new Aggregate(AggType.Sum, column, source)));
        Assert.True(hook.CanHandle(new GroupBy([], [], source)));
        Assert.True(hook.CanHandle(new OrderBy(key, true, source)));
        Assert.True(hook.CanHandle(new Limit(count, source)));
        Assert.True(hook.CanHandle(new TableStats("users", 1000, new Dictionary<string, ColumnStats>())));
    }

    [Fact]
    public void DataCostHook_ScanCost_IsExpensive()
    {
        var hook = new DataCostHook();
        var scanCost = hook.Estimate(new Scan("users"));
        Assert.True(scanCost.Latency >= 1000, "Scan 成本应 >= 1000");
    }

    [Fact]
    public void DataCostHook_IndexScanCost_CheaperThanScan()
    {
        var hook = new DataCostHook();
        var egraph = create_e_graph();
        var pred = egraph.add(new Literal<long>(0));
        var scanCost = hook.Estimate(new Scan("users"));
        var indexScanCost = hook.Estimate(new IndexScan("users", "idx", pred));
        Assert.True(indexScanCost.Latency < scanCost.Latency, "IndexScan 应比 Scan 便宜");
    }

    [Fact]
    public void DataCostHook_JoinCost_IsExpensive()
    {
        var hook = new DataCostHook();
        var egraph = create_e_graph();
        var left = egraph.add(new Literal<long>(0));
        var right = egraph.add(new Literal<long>(1));
        var cond = egraph.add(new Literal<long>(2));
        var joinCost = hook.Estimate(new Join(JoinType.Inner, left, right, cond));
        Assert.True(joinCost.Latency >= 500, "Join 成本应 >= 500");
    }

    [Fact]
    public void DataCostHook_FilterCost_CheaperThanJoin()
    {
        var hook = new DataCostHook();
        var egraph = create_e_graph();
        var pred = egraph.add(new Literal<long>(0));
        var source = egraph.add(new Literal<long>(1));
        var left = egraph.add(new Literal<long>(2));
        var right = egraph.add(new Literal<long>(3));
        var cond = egraph.add(new Literal<long>(4));
        var filterCost = hook.Estimate(new Filter(pred, source));
        var joinCost = hook.Estimate(new Join(JoinType.Inner, left, right, cond));
        Assert.True(filterCost.Latency < joinCost.Latency, "Filter 应比 Join 便宜");
    }

    [Fact]
    public void DataBuiltin_HasCorrectValues()
    {
        Assert.Equal(0xA001L, (long)DataBuiltin.Scan);
        Assert.Equal(0xA005L, (long)DataBuiltin.Join);
        Assert.Equal(0xA006L, (long)DataBuiltin.Aggregate);
        Assert.Equal(0xA00FL, (long)DataBuiltin.Union);
    }
}