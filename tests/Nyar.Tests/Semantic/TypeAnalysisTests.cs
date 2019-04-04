using Nyar.Dialect.Core;
using Nyar.Dialect.Core.Analysis;
using Nyar.Dialect.Core.Nodes;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.Tests.Semantic;

public class TypeAnalysisTests
{
    private static EGraph<AlgebraNode> create_analyzed_e_graph()
    {
        var analysis = new IKunTypeAnalysis();
        return new EGraph<AlgebraNode>(analysis);
    }

    [Fact]
    public void Constant_GetsIntegerType()
    {
        var egraph = create_analyzed_e_graph();
        var id = egraph.add(new Literal<long>(42));

        var eclass = egraph.get_class(id);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Equal(TypeKind.integer, info.Type.kind);
    }

    [Fact]
    public void FloatConstant_GetsFloatType()
    {
        var egraph = create_analyzed_e_graph();
        var id = egraph.add(new Literal<double>(0D));

        var eclass = egraph.get_class(id);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Equal(TypeKind.@float, info.Type.kind);
    }

    [Fact]
    public void BooleanConstant_GetsBooleanType()
    {
        var egraph = create_analyzed_e_graph();
        var id = egraph.add(new Literal<bool>(true));

        var eclass = egraph.get_class(id);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Equal(TypeKind.boolean, info.Type.kind);
    }

    [Fact]
    public void Add_GetsTypeFromOperands()
    {
        var egraph = create_analyzed_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Add(a, b));

        var eclass = egraph.get_class(add);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Equal(TypeKind.integer, info.Type.kind);
    }

    [Fact]
    public void Cmp_GetsBooleanType()
    {
        var egraph = create_analyzed_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var cmp = egraph.add(new Cmp(CompareOp.lt, a, b));

        var eclass = egraph.get_class(cmp);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Equal(TypeKind.boolean, info.Type.kind);
    }

    [Fact]
    public void Load_HasReadEffect()
    {
        var egraph = create_analyzed_e_graph();
        var ptr = egraph.add(new Literal<long>(0));
        var load = egraph.add(new Load(ptr, MemoryOrder.None));

        var eclass = egraph.get_class(load);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Contains(EffectKind.read, info.Effects.effects);
    }

    [Fact]
    public void Store_HasWriteEffect()
    {
        var egraph = create_analyzed_e_graph();
        var ptr = egraph.add(new Literal<long>(0));
        var val = egraph.add(new Literal<long>(1));
        var store = egraph.add(new Store(ptr, val, MemoryOrder.None));

        var eclass = egraph.get_class(store);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Contains(EffectKind.write, info.Effects.effects);
    }

    [Fact]
    public void PureOperations_HaveNoEffects()
    {
        var egraph = create_analyzed_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Add(a, b));

        var eclass = egraph.get_class(add);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.True(info.Effects.is_pure);
    }

    [Fact]
    public void Union_MergesEffects()
    {
        var egraph = create_analyzed_e_graph();
        var a = egraph.add(new Literal<long>(1));
        var b = egraph.add(new Literal<long>(2));
        var add = egraph.add(new Add(a, b));
        var load = egraph.add(new Load(a, MemoryOrder.None));

        egraph.union(add, load);
        egraph.rebuild();

        var eclass = egraph.get_class(add);
        Assert.NotNull(eclass);
        var info = Assert.IsType<IKunClassInfo>(eclass!.data);
        Assert.Contains(EffectKind.read, info.Effects.effects);
    }

    [Fact]
    public void IncompatibleTypes_UnionRejected()
    {
        var egraph = create_analyzed_e_graph();
        var intVal = egraph.add(new Literal<long>(42));
        var boolVal = egraph.add(new Literal<bool>(true));

        var root = egraph.union(intVal, boolVal);

        var intRoot = egraph.union_find.find(intVal);
        var boolRoot = egraph.union_find.find(boolVal);

        Assert.NotEqual(intRoot, boolRoot);
    }
}