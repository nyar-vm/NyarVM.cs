using Nyar.Dialect.Core.Nodes;
using System.Diagnostics;
using Nyar.Dialect;
using Nyar.Dialect.Core;
using Nyar.Dialect.Standard;
using Nyar.Dialect.Web;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Nyar.ObjectAlgebra;

namespace Nyar.Tests.Dialects;

public class DialectPerformanceTests
{
    #region EGraph 饱和时间基准

    [Theory]
    [InlineData("core", typeof(CoreDialect))]
    [InlineData("std", typeof(StandardDialect))]
    [InlineData("game", typeof(GameDialect))]
    [InlineData("shader", typeof(ShaderDialect))]
    [InlineData("data", typeof(DataDialect))]
    [InlineData("web", typeof(WebDialect))]
    [InlineData("hardware", typeof(HardwareDialect))]
    public void EGraph_SaturationTime_UnderThreshold(string dialectName, Type dialectType)
    {
        var dialect = (IDialect)Activator.CreateInstance(dialectType)!;
        var egraph = new EGraph<Oa>();

        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Apply(
            egraph.add(new Sym($"{dialectName}.test_op")),
            [a, b]
        ));

        var sw = Stopwatch.StartNew();

        foreach (var rule in dialect.rules)
        {
            foreach (var ec in egraph.classes())
            {
                rule.apply(egraph, ec.Id);
            }
        }

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 1000,
            $"方言 '{dialectName}' EGraph 饱和耗时 {sw.ElapsedMilliseconds}ms 超过阈值 1000ms");
    }

    #endregion

    #region 降级规则执行基准

    [Fact(Skip = "pe_factories 类型已变为 IReadOnlyList<IPEFactory>，不再支持 rule.apply 调用")]
    public void LoweringRules_ExecutionTime_UnderThreshold()
    {
        // pe_factories 现在返回 IReadOnlyList<IPEFactory>，不再包含 IRewriteRule，
        // 因此降级规则执行基准测试暂不适用
    }

    #endregion

    #region 规则计数基准

    [Fact]
    public void AllDialects_RuleCount_Summary()
    {
        var dialects = new IDialect[]
        {
            new CoreDialect(),
            new StandardDialect(),
            new GameDialect(),
            new ShaderDialect(),
            new DataDialect(),
            new WebDialect(),
            new HardwareDialect()
        };

        var totalRules = 0;
        var totalPEFactories = 0;
        var totalCostHooks = 0;

        foreach (var dialect in dialects)
        {
            totalRules += dialect.rules.Count;
            totalPEFactories += dialect.pe_factories.Count;
            totalCostHooks += dialect.cost_hooks.Count;
        }

        Assert.True(totalRules > 0);
        Assert.Equal(0, totalPEFactories);
        Assert.True(totalCostHooks > 0);
    }

    #endregion

    #region DialectRegistry 聚合基准

    [Fact]
    public void DialectRegistry_FullRegistration_UnderThreshold()
    {
        var sw = Stopwatch.StartNew();

        var registry = new DialectRegistry();
        registry
            .register(new CoreDialect())
            .register(new StandardDialect())
            .register(new GameDialect())
            .register(new ShaderDialect())
            .register(new DataDialect())
            .register(new WebDialect())
            .register(new HardwareDialect());

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100,
            $"DialectRegistry 全方言注册耗时 {sw.ElapsedMilliseconds}ms 超过阈值 100ms");
    }

    [Fact]
    public void DialectRegistry_FreezeAndValidate_UnderThreshold()
    {
        var registry = new DialectRegistry();
        registry
            .register(new CoreDialect())
            .register(new StandardDialect())
            .register(new GameDialect())
            .register(new ShaderDialect())
            .register(new DataDialect())
            .register(new WebDialect())
            .register(new HardwareDialect());

        var sw = Stopwatch.StartNew();
        registry.freeze();
        registry.validate_dependencies(out _);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 50,
            $"DialectRegistry 冻结+验证耗时 {sw.ElapsedMilliseconds}ms 超过阈值 50ms");
    }

    [Fact]
    public void DialectRegistry_TopologicalSort_UnderThreshold()
    {
        var registry = new DialectRegistry();
        registry
            .register(new CoreDialect())
            .register(new StandardDialect())
            .register(new GameDialect())
            .register(new ShaderDialect())
            .register(new DataDialect())
            .register(new WebDialect())
            .register(new HardwareDialect());

        var sw = Stopwatch.StartNew();
        var order = registry.get_topological_order();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 50,
            $"DialectRegistry 拓扑排序耗时 {sw.ElapsedMilliseconds}ms 超过阈值 50ms");
        Assert.Equal(7, order.Count);
    }

    #endregion

    #region Extractor 提取质量基准

    [Fact]
    public void Extractor_SimpleExpression_ExtractsCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Apply(
            egraph.add(new Sym("core.add")),
            [a, b]
        ));

        var registry = new DialectRegistry();
        registry.register(new CoreDialect());
        var costModel = registry.build_cost_model();

        var sw = Stopwatch.StartNew();
        var extractor = new Extractor(egraph, costModel);
        var tree = extractor.extract(add);
        sw.Stop();

        Assert.NotNull(tree);
        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Extractor 提取耗时 {sw.ElapsedMilliseconds}ms 超过阈值 100ms");
    }

    [Fact]
    public void Extractor_NestedExpression_ExtractsCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var one = egraph.add(new Literal<long>(1));
        var two = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Apply(
            egraph.add(new Sym("core.add")),
            [one, two]
        ));
        var three = egraph.add(new Literal<long>(3));
        var mul = egraph.add(new Apply(
            egraph.add(new Sym("core.mul")),
            [add, three]
        ));

        var registry = new DialectRegistry();
        registry.register(new CoreDialect());
        var costModel = registry.build_cost_model();

        var sw = Stopwatch.StartNew();
        var extractor = new Extractor(egraph, costModel);
        var tree = extractor.extract(mul);
        sw.Stop();

        Assert.NotNull(tree);
        Assert.True(sw.ElapsedMilliseconds < 200,
            $"Extractor 嵌套表达式提取耗时 {sw.ElapsedMilliseconds}ms 超过阈值 200ms");
    }

    #endregion
}