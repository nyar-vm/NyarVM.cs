using System.Collections.Immutable;
using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Core.Rules;
using Nyar.Dialect.Standard;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Physical;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.EGraph;

/// <summary>
///     去虚拟化重写规则测试
/// </summary>
public class DevirtualizationRuleTests
{
    private static EGraph<AlgebraNode> create_e_graph()
    {
        return new EGraph<AlgebraNode>(null);
    }

    #region DevirtualizationRule 测试

    [Fact]
    public void CallDynamic_WithWitness_PromotesToWitness()
    {
        var egraph = create_e_graph();
        var receiver = egraph.add(new Literal<long>(0));
        var witness = egraph.add(new Literal<long>(1));
        var args = ImmutableArray<Id>.Empty;
        var call = egraph.add(new PhysicalNode.Call(DispatchKind.dynamic, receiver, args, witness));

        var engine = new SaturationEngine<AlgebraNode>([new DevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(call);
        Assert.NotNull(eclass);
        var hasWitnessCall = eclass!.nodes.Any(n =>
            n is PhysicalNode.Call { dispatch: DispatchKind.witness });
        Assert.True(hasWitnessCall, "Dynamic Call 有 Witness 引用时应降级为 Witness");
    }

    [Fact]
    public void CallDynamic_WithMethodIndex_PromotesToStatic()
    {
        var egraph = create_e_graph();
        var target = egraph.add(new Literal<long>(0));
        var args = ImmutableArray<Id>.Empty;
        var call = egraph.add(new PhysicalNode.Call(DispatchKind.dynamic, target, args)
        {
            method_index = 3
        });

        var engine = new SaturationEngine<AlgebraNode>([new DevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(call);
        Assert.NotNull(eclass);
        var hasStaticCall = eclass!.nodes.Any(n =>
            n is PhysicalNode.Call { dispatch: DispatchKind.@static });
        Assert.True(hasStaticCall, "Dynamic Call 有 MethodIndex 时应降级为 Static");
    }

    [Fact]
    public void CallWitness_WithMethodIndex_PromotesToStatic()
    {
        var egraph = create_e_graph();
        var receiver = egraph.add(new Literal<long>(0));
        var witness = egraph.add(new Literal<long>(1));
        var args = ImmutableArray<Id>.Empty;
        var call = egraph.add(new PhysicalNode.Call(DispatchKind.witness, receiver, args, witness)
        {
            method_index = 5
        });

        var engine = new SaturationEngine<AlgebraNode>([new DevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(call);
        Assert.NotNull(eclass);
        var hasStaticCall = eclass!.nodes.Any(n =>
            n is PhysicalNode.Call { dispatch: DispatchKind.@static });
        Assert.True(hasStaticCall, "Witness Call 有 MethodIndex 时应降级为 Static");
    }

    [Fact]
    public void CallDynamic_NoWitnessNoMethodIndex_StaysDynamic()
    {
        var egraph = create_e_graph();
        var receiver = egraph.add(new Literal<long>(0));
        var args = ImmutableArray<Id>.Empty;
        var call = egraph.add(new PhysicalNode.Call(DispatchKind.dynamic, receiver, args)
        {
            method_name = "foo"
        });

        var engine = new SaturationEngine<AlgebraNode>([new DevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.Equal(0, result.total_unions);
    }

    [Fact]
    public void CallStatic_NotAffectedByRule()
    {
        var egraph = create_e_graph();
        var target = egraph.add(new Literal<long>(0));
        var args = ImmutableArray<Id>.Empty;
        var call = egraph.add(new PhysicalNode.Call(DispatchKind.@static, target, args)
        {
            method_index = 1
        });

        var engine = new SaturationEngine<AlgebraNode>([new DevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.Equal(0, result.total_unions);
    }

    #endregion

    #region AccessDevirtualizationRule 测试

    [Fact]
    public void AccessDynamic_WithWitness_PromotesToWitness()
    {
        var egraph = create_e_graph();
        var obj = egraph.add(new Literal<long>(0));
        var witness = egraph.add(new Literal<long>(1));
        var access = egraph.add(new PhysicalNode.Access(DispatchKind.dynamic, obj, 0, "field", witness));

        var engine = new SaturationEngine<AlgebraNode>([new AccessDevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(access);
        Assert.NotNull(eclass);
        var hasWitnessAccess = eclass!.nodes.Any(n =>
            n is PhysicalNode.Access { dispatch: DispatchKind.witness });
        Assert.True(hasWitnessAccess, "Dynamic Access 有 Witness 引用时应降级为 Witness");
    }

    [Fact]
    public void AccessDynamic_WithFieldIndex_PromotesToStatic()
    {
        var egraph = create_e_graph();
        var obj = egraph.add(new Literal<long>(0));
        var access = egraph.add(new PhysicalNode.Access(DispatchKind.dynamic, obj, 3, "field"));

        var engine = new SaturationEngine<AlgebraNode>([new AccessDevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(access);
        Assert.NotNull(eclass);
        var hasStaticAccess = eclass!.nodes.Any(n =>
            n is PhysicalNode.Access { dispatch: DispatchKind.@static });
        Assert.True(hasStaticAccess, "Dynamic Access 有 FieldIndex > 0 时应降级为 Static");
    }

    [Fact]
    public void AccessWitness_WithFieldIndex_PromotesToStatic()
    {
        var egraph = create_e_graph();
        var obj = egraph.add(new Literal<long>(0));
        var witness = egraph.add(new Literal<long>(1));
        var access = egraph.add(new PhysicalNode.Access(DispatchKind.witness, obj, 2, null, witness));

        var engine = new SaturationEngine<AlgebraNode>([new AccessDevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.True(result.total_unions > 0);

        var eclass = egraph.get_class(access);
        Assert.NotNull(eclass);
        var hasStaticAccess = eclass!.nodes.Any(n =>
            n is PhysicalNode.Access { dispatch: DispatchKind.@static });
        Assert.True(hasStaticAccess, "Witness Access 有 FieldIndex > 0 时应降级为 Static");
    }

    [Fact]
    public void AccessDynamic_NoWitnessZeroFieldIndex_StaysDynamic()
    {
        var egraph = create_e_graph();
        var obj = egraph.add(new Literal<long>(0));
        var access = egraph.add(new PhysicalNode.Access(DispatchKind.dynamic, obj, 0, "field"));

        var engine = new SaturationEngine<AlgebraNode>([new AccessDevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.Equal(0, result.total_unions);
    }

    [Fact]
    public void AccessStatic_NotAffectedByRule()
    {
        var egraph = create_e_graph();
        var obj = egraph.add(new Literal<long>(0));
        var access = egraph.add(new PhysicalNode.Access(DispatchKind.@static, obj, 5));

        var engine = new SaturationEngine<AlgebraNode>([new AccessDevirtualizationRule()]);
        var result = engine.run(egraph);

        Assert.Equal(0, result.total_unions);
    }

    #endregion

    #region 集成测试：StandardDialect 包含去虚拟化规则

    [Fact]
    public void StandardDialect_ContainsDevirtualizationRules()
    {
        var dialect = new StandardDialect();
        var ruleNames = dialect.rules.Select(r => r.name).ToList();

        Assert.Contains("devirtualization", ruleNames);
        Assert.Contains("access-devirtualization", ruleNames);
    }

    [Fact]
    public void StandardDialect_CallDynamicWithWitness_Devirtualizes()
    {
        var egraph = create_e_graph();
        var receiver = egraph.add(new Literal<long>(0));
        var witness = egraph.add(new Literal<long>(1));
        var args = ImmutableArray<Id>.Empty;
        var call = egraph.add(new PhysicalNode.Call(DispatchKind.dynamic, receiver, args, witness));

        var dialect = new StandardDialect();
        var engine = new SaturationEngine<AlgebraNode>(dialect.rules);
        engine.run(egraph);

        var eclass = egraph.get_class(call);
        Assert.NotNull(eclass);
        var hasWitnessCall = eclass!.nodes.Any(n =>
            n is PhysicalNode.Call { dispatch: DispatchKind.witness });
        Assert.True(hasWitnessCall, "Standard 方言应将 Dynamic Call 去虚拟化为 Witness");
    }

    #endregion
}