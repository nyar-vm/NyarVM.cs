using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using ScheduleTask = Nyar.Dialect.Schedule.Nodes.Task;

namespace Nyar.Tests.Dialects;

/// <summary>
///     Schedule 方言集成测试
/// </summary>
public class ScheduleDialectIntegrationTests
{
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact]
    public void ScheduleDialect_HasCorrectStructure()
    {
        var dialect = new ScheduleDialect();
        Assert.Equal(4, dialect.rules.Count);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact]
    public void ScheduleDialect_CostHook_HandlesKeyNodes()
    {
        var hook = new ScheduleCostHook();
        var egraph = create_e_graph();
        var duration = egraph.add(new Literal<long>(10));
        var scheduleRef = egraph.add(new Literal<long>(0));

        Assert.True(hook.can_handle(new ScheduleTask("task1", duration, [], 0)));
        Assert.True(hook.can_handle(new ResourcePool("cpu", "compute", 4, [])));
        Assert.True(hook.can_handle(new MinimizeMakespan(scheduleRef)));
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Task_LowersToApply()
    {
        var egraph = create_e_graph();
        var duration = egraph.add(new Literal<long>(10));
        var nodeId = egraph.add(new ScheduleTask("task1", duration, [], 0));

        var dialect = new ScheduleDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Task 应降级为 Apply");
    }

    [Fact]
    public void ScheduleBuiltin_HasCorrectValues()
    {
        Assert.Equal(0xE001L, (long)ScheduleBuiltin.TaskDef);
        Assert.Equal(0xE101L, (long)ScheduleBuiltin.ObjMinimizeMakespan);
        Assert.Equal(0xE201L, (long)ScheduleBuiltin.StrategyDef);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AllScheduleNodes_LowerToApply()
    {
        var egraph = create_e_graph();
        var duration = egraph.add(new Literal<long>(10));
        var task1 = egraph.add(new Literal<long>(0));
        var task2 = egraph.add(new Literal<long>(1));
        var earliest = egraph.add(new Literal<long>(0));
        var latest = egraph.add(new Literal<long>(100));
        var constraint = egraph.add(new Literal<long>(0));
        var scheduleRef = egraph.add(new Literal<long>(0));
        var problem = egraph.add(new Literal<long>(0));

        var nodes = new Oa[]
        {
            new ScheduleTask("task1", duration, [], 0),
            new TaskSet([task1, task2]),
            new Precedence(task1, task2),
            new TimeWindow(task1, earliest, latest),
            new MutualExclusion(task1, task2),
            new CoLocation(task1, task2),
            new SoftConstraint(constraint, 0.5f),
            new ResourcePool("cpu", "compute", 4, []),
            new ResourceAssignment(task1, "cpu", 2),
            new MinimizeMakespan(scheduleRef),
            new MinimizeTotalDelay(scheduleRef),
            new MinimizeResourceCost(scheduleRef, new Dictionary<string, float> { ["cpu"] = 1.0f }),
            new BalanceLoad(scheduleRef),
            new MultiObjective([scheduleRef], [1.0f]),
            new ScheduleStrategy(ScheduleAlgorithm.ListScheduling, problem),
            new ScheduleResult(new Dictionary<string, TimeSlot>())
        };

        foreach (var node in nodes)
        {
            egraph.add(node);
        }

        var dialect = new ScheduleDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 16, "全部 16 个 Schedule 节点都应产生降级");
    }
}