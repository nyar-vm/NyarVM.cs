using Nyar.Analyzer.Semantic;

namespace Nyar.Tests.Compiler;

public sealed class SemanticModelSnapshotTests : IncrementalCacheTestBase
{
    [Fact]
    public void SemanticModelSnapshot_RoundTrip_PreservesBindingsTypesAndReferences()
    {
        var semanticModel = context.create_demo_semantic_model();

        var snapshot = SemanticModelSnapshotSerializer.serialize(semanticModel);
        var restored = SemanticModelSnapshotSerializer.deserialize(snapshot);

        Assert.Equal("test.v", restored.file_path);
        Assert.Single(restored.diagnostics);
        Assert.Equal("测试诊断", restored.diagnostics[0].message);

        var restoredFunction = Assert.IsType<Symbol>(restored.get_declared_symbol(1));
        Assert.Equal("makePoint", restoredFunction.name);
        Assert.True(restoredFunction.is_static);
        Assert.Equal("demo", restoredFunction.containing_scope?.name);

        var restoredNamedType = Assert.IsType<NamedType>(restored.get_type_info(2));
        Assert.Equal("Point", restoredNamedType.name);
        Assert.Single(restoredNamedType.members);
        Assert.Equal("value", restoredNamedType.members[0].name);

        var restoredFunctionType = Assert.IsType<FunctionType>(restored.get_type_info(1));
        Assert.Equal(2, restoredFunctionType.parameter_types.Count);
        Assert.IsType<RowType>(restoredFunctionType.return_type);

        var restoredReferences = restored.find_references(restoredFunction);
        Assert.Single(restoredReferences);
        Assert.Equal("value", restoredReferences[0].name);
    }
}
