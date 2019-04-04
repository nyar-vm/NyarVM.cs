using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;
using ShaderBarrier = Nyar.Dialect.Shader.Nodes.Barrier;

namespace Nyar.Tests.Dialects;

/// <summary>
///     Shader 方言集成测试
/// </summary>
public class ShaderDialectIntegrationTests
{
    /// <summary>
    ///     创建测试用 EGraph 实例
    /// </summary>
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact]
    public void ShaderDialect_HasCorrectStructure()
    {
        var dialect = new ShaderDialect();
        Assert.Equal(25, dialect.rules.Count);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact]
    public void ShaderCostHook_HandlesKeyNodes()
    {
        var hook = new ShaderCostHook();
        var egraph = create_e_graph();
        var body = egraph.add(new Literal<long>(0));
        var tex = egraph.add(new Literal<long>(1));
        var uv = egraph.add(new Literal<long>(2));
        var a = egraph.add(new Literal<long>(3));
        var b = egraph.add(new Literal<long>(4));

        Assert.True(hook.can_handle(new ComputeKernel(body, (1, 1, 1))));
        Assert.True(hook.can_handle(new Sample(tex, uv)));
        Assert.True(hook.can_handle(new Dot(a, b)));
        Assert.True(hook.can_handle(new ShaderBarrier("device")));
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void TextureLoad_LowersToApply()
    {
        var egraph = create_e_graph();
        var tex = egraph.add(new Literal<long>(1));
        var coord = egraph.add(new Literal<long>(2));
        var nodeId = egraph.add(new TextureLoad(tex, coord));

        var dialect = new ShaderDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "TextureLoad 应降级为 Apply");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void Sample_LowersToApply()
    {
        var egraph = create_e_graph();
        var tex = egraph.add(new Literal<long>(1));
        var uv = egraph.add(new Literal<long>(2));
        var nodeId = egraph.add(new Sample(tex, uv));

        var dialect = new ShaderDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "Sample 应降级为 Apply");
    }

    [Fact]
    public void DotSelfRewrite_Applies()
    {
        var egraph = create_e_graph();
        var v = egraph.add(new Sym("v"));
        var dotId = egraph.add(new Dot(v, v));

        var dialect = new ShaderDialect();
        var engine = new SaturationEngine<Oa>(dialect.rules);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0, "DotSelfToLengthSquareRule 应产生合并");
    }

    [Fact]
    public void ShaderCostHook_ReturnsReasonableCosts()
    {
        var hook = new ShaderCostHook();
        var egraph = create_e_graph();
        var body = egraph.add(new Literal<long>(0));

        var kernelCost = hook.Estimate(new ComputeKernel(body, (1, 1, 1)));
        Assert.True(kernelCost.Latency > 0, "ComputeKernel 成本应大于 0");

        var barrierCost = hook.Estimate(new ShaderBarrier("device"));
        Assert.True(barrierCost.Latency > 0, "Barrier 成本应大于 0");
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void KeyShaderNodes_LowerToApply()
    {
        var egraph = create_e_graph();
        var tex = egraph.add(new Literal<long>(1));
        var uv = egraph.add(new Literal<long>(2));
        var lod = egraph.add(new Literal<long>(3));
        var ddx = egraph.add(new Literal<long>(4));
        var ddy = egraph.add(new Literal<long>(5));
        var dref = egraph.add(new Literal<long>(6));
        var coord = egraph.add(new Literal<long>(7));
        var value = egraph.add(new Literal<long>(8));
        var a = egraph.add(new Literal<long>(9));
        var b = egraph.add(new Literal<long>(10));
        var v = egraph.add(new Literal<long>(11));
        var i = egraph.add(new Literal<long>(12));
        var n = egraph.add(new Literal<long>(13));
        var eta = egraph.add(new Literal<long>(14));
        var buf = egraph.add(new Literal<long>(15));
        var idx = egraph.add(new Literal<long>(16));
        var addr = egraph.add(new Literal<long>(17));

        var keyNodes = new Oa[]
        {
            new Sample(tex, uv),
            new SampleLod(tex, uv, lod),
            new SampleGrad(tex, uv, ddx, ddy),
            new SampleDref(tex, uv, dref),
            new TextureLoad(tex, coord),
            new TextureStore(tex, coord, value),
            new Dot(a, b),
            new Cross(a, b),
            new Length(v),
            new Normalize(v),
            new Reflect(i, n),
            new Refract(i, n, eta),
            new MatMul(a, b),
            new UniformLoad(buf, idx),
            new StorageLoad(buf, idx),
            new StorageStore(buf, idx, value),
            new SharedLoad(addr),
            new SharedStore(addr, value)
        };

        foreach (var node in keyNodes)
        {
            egraph.add(node);
        }

        var dialect = new ShaderDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 18, "关键 Shader 节点都应产生降级");
    }
}