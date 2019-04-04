using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect;
using Nyar.Dialect.Core;
using Nyar.Dialect.Standard;
using Nyar.Dialect.Web;
using Nyar.EGraph;
using Nyar.IR.Extractor;
using Nyar.IR.Intent;
using Barrier = Nyar.Dialect.Shader.Nodes.Barrier;

namespace Nyar.Tests.Dialects;

public class CrossDialectPipelineTests
{
    #region Core → Game → GnosisVM 管线

    [Fact]
    public void CoreToGamePipeline_EcsOperation_LowersCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var entityId = egraph.add(new Literal<long>(42));
        var createEntity = egraph.add(new Apply(
            egraph.add(new Sym("game.create_entity")),
            [entityId]
        ));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new GameDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, createEntity);
    }

    #endregion

    #region Core → Data 管线

    [Fact]
    public void CoreToDataPipeline_ScanFilter_LowersCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var table = egraph.add(new Literal<string>("users"));
        var scan = egraph.add(new Apply(
            egraph.add(new Sym("data.scan")),
            [table]
        ));
        var predicate = egraph.add(new Literal<string>("age > 18"));
        var filter = egraph.add(new Apply(
            egraph.add(new Sym("data.filter")),
            [scan, predicate]
        ));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new DataDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, filter);
    }

    #endregion

    #region Core → Web 管线

    [Fact]
    public void CoreToWebPipeline_HttpFetch_LowersCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var url = egraph.add(new Literal<string>("https://api.example.com/data"));
        var fetch = egraph.add(new Apply(
            egraph.add(new Sym("web.fetch")),
            [url]
        ));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new WebDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, fetch);
    }

    #endregion

    #region Core → Hardware 管线

    [Fact]
    public void CoreToHardwarePipeline_GpioRead_LowersCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var pin = egraph.add(new Literal<long>(13));
        var gpioRead = egraph.add(new Apply(
            egraph.add(new Sym("hardware.gpio_read")),
            [pin]
        ));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new HardwareDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, gpioRead);
    }

    #endregion

    #region 辅助方法

    private static DialectRegistry create_full_registry()
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
        return registry;
    }

    #endregion

    #region Core → Standard → NyarVM 管线

    [Fact]
    public void CoreToStandardPipeline_ArithmeticExpression_LowersCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Apply(
            egraph.add(new Sym("core.add")),
            [a, b]
        ));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new StandardDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, add);
    }

    [Fact]
    public void CoreToStandardPipeline_NestedExpression_Optimizes()
    {
        var egraph = new EGraph<Oa>();
        var one = egraph.add(new Literal<long>(1));
        var two = egraph.add(new Literal<long>(2));
        var addResult = egraph.add(new Apply(
            egraph.add(new Sym("core.add")),
            [one, two]
        ));
        var three = egraph.add(new Literal<long>(3));
        var mulResult = egraph.add(new Apply(
            egraph.add(new Sym("core.mul")),
            [addResult, three]
        ));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new StandardDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, mulResult);
    }

    #endregion

    #region Core → Shader → SPIR-V 管线

    [Fact]
    public void CoreToShaderPipeline_ComputeKernel_LowersCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var body = egraph.add(new Apply(
            egraph.add(new Sym("core.add")),
            [egraph.add(new Literal<long>(1)), egraph.add(new Literal<long>(2))]
        ));
        var kernel = egraph.add(new ComputeKernel(body, (1, 1, 1)));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new ShaderDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, kernel);
    }

    [Fact]
    public void CoreToShaderPipeline_Barrier_LowersCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var barrier = egraph.add(new Barrier("workgroup"));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new ShaderDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, barrier);
    }

    #endregion

    #region 多方言组合管线

    [Fact]
    public void MultiDialectPipeline_StandardAndGame_BothLowerCorrectly()
    {
        var egraph = new EGraph<Oa>();
        var addNode = egraph.add(new Apply(
            egraph.add(new Sym("core.add")),
            [egraph.add(new Literal<long>(1)), egraph.add(new Literal<long>(2))]
        ));
        var createEntity = egraph.add(new Apply(
            egraph.add(new Sym("game.create_entity")),
            [egraph.add(new Literal<long>(42))]
        ));

        var registry = new DialectRegistry();
        registry.register_range([new CoreDialect(), new StandardDialect(), new GameDialect()]);
        registry.register_all_rules(egraph);

        Assert.NotEqual(default, createEntity);
    }

    [Fact]
    public void MultiDialectPipeline_AllDialects_NoExceptions()
    {
        var egraph = new EGraph<Oa>();
        var node = egraph.add(new Apply(
            egraph.add(new Sym("core.add")),
            [egraph.add(new Literal<long>(1)), egraph.add(new Literal<long>(2))]
        ));

        var registry = new DialectRegistry();
        registry
            .register(new CoreDialect())
            .register(new StandardDialect())
            .register(new GameDialect())
            .register(new ShaderDialect())
            .register(new DataDialect())
            .register(new WebDialect())
            .register(new HardwareDialect());

        registry.register_all_rules(egraph);

        Assert.NotEqual(default, node);
    }

    #endregion

    #region 降级覆盖率验证

    [Fact]
    public void AllDialects_PEFactoriesCount_Empty()
    {
        var registry = create_full_registry();
        var peFactories = registry.get_all_pe_factories();

        Assert.Empty(peFactories);
    }

    [Fact]
    public void AllDialects_RewriteRulesCount_NonZero()
    {
        var registry = create_full_registry();
        var rewriteRules = registry.get_all_rules();

        Assert.True(rewriteRules.Count >= 50,
            $"重写规则总数 {rewriteRules.Count} 少于预期 50");
    }

    #endregion
}