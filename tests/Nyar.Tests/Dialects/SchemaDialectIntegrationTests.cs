using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Dialects;

/// <summary>
///     Schema 方言集成测试
/// </summary>
public class SchemaDialectIntegrationTests
{
    private static EGraph<Oa> create_e_graph()
    {
        return new EGraph<Oa>(null);
    }

    [Fact]
    public void SchemaDialect_HasCorrectStructure()
    {
        var dialect = new SchemaDialect();
        Assert.Empty(dialect.rules);
        Assert.Empty(dialect.pe_factories);
        Assert.Single(dialect.cost_hooks);
    }

    [Fact]
    public void SchemaDialect_CostHook_HandlesKeyNodes()
    {
        var hook = new SchemaCostHook();
        var egraph = create_e_graph();
        var keyType = egraph.add(new Literal<long>(0));
        var fields = egraph.add(new Literal<long>(0));
        var fieldType = egraph.add(new Literal<long>(0));
        var endpoints = egraph.add(new Literal<long>(0));
        var models = egraph.add(new Literal<long>(0));
        var streams = egraph.add(new Literal<long>(0));
        var caches = egraph.add(new Literal<long>(0));
        var parameters = egraph.add(new Literal<long>(0));
        var returnType = egraph.add(new Literal<long>(0));

        Assert.True(hook.CanHandle(new SchemaModel("User", keyType, fields)));
        Assert.True(hook.CanHandle(new SchemaField("name", fieldType, false)));
        Assert.True(hook.CanHandle(new SchemaService("UserService", endpoints)));
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void SchemaModel_LowersToApply()
    {
        var egraph = create_e_graph();
        var keyType = egraph.add(new Literal<long>(0));
        var fields = egraph.add(new Literal<long>(0));
        var nodeId = egraph.add(new SchemaModel("User", keyType, fields));

        var dialect = new SchemaDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);
        var eclass = egraph.get_class(nodeId);
        Assert.NotNull(eclass);
        Assert.True(eclass!.nodes.Any(n => n is Apply), "SchemaModel 应降级为 Apply");
    }

    [Fact]
    public void SchemaBuiltin_HasCorrectValues()
    {
        Assert.Equal(0xB001L, (long)SchemaBuiltin.ModelDef);
        Assert.Equal(0xB006L, (long)SchemaBuiltin.GrpcEndpointDef);
    }

    [Fact(Skip = "pe_factories 返回空数组，降级规则测试暂不适用")]
    public void AllSchemaNodes_LowerToApply()
    {
        var egraph = create_e_graph();
        var keyType = egraph.add(new Literal<long>(0));
        var fields = egraph.add(new Literal<long>(0));
        var fieldType = egraph.add(new Literal<long>(0));
        var models = egraph.add(new Literal<long>(0));
        var streams = egraph.add(new Literal<long>(0));
        var caches = egraph.add(new Literal<long>(0));
        var endpoints = egraph.add(new Literal<long>(0));
        var parameters = egraph.add(new Literal<long>(0));
        var returnType = egraph.add(new Literal<long>(0));

        var nodes = new Oa[]
        {
            new SchemaModel("User", keyType, fields),
            new SchemaField("name", fieldType, false),
            new SchemaStorage("postgres", models, streams, caches),
            new SchemaService("UserService", endpoints),
            new SchemaHttpEndpoint("GET", "/users", parameters, returnType),
            new SchemaGrpcEndpoint("UserService", "GetUser", parameters, returnType)
        };

        foreach (var node in nodes)
        {
            egraph.add(node);
        }

        var dialect = new SchemaDialect();
        var engine = new SaturationEngine<Oa>(dialect.pe_factories);
        var result = engine.run(egraph);

        Assert.True(result.total_unions >= 6, "全部 6 个 Schema 节点都应产生降级");
    }
}