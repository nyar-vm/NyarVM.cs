using Nyar.Language.Valkyrie.Compiler.Hir;

namespace Valkyrie.Tests.CompilerTests;

public sealed class HirTypeNameTests
{
    [Fact]
    public void Parse_ArrayTypeName_ShouldUseStructuredPredicate()
    {
        Assert.True(HirNamePath.parse("Array").is_array_type);
        Assert.True(HirNamePath.parse("std::collection::Array").is_array_type);
        Assert.False(HirNamePath.parse("ArrayList").is_array_type);
    }

    [Fact]
    public void Parse_TupleAndUnknownTypeName_ShouldUseStructuredPredicate()
    {
        Assert.True(HirNamePath.parse("tuple").is_tuple_type);
        Assert.False(HirNamePath.parse("Tuple").is_tuple_type);
        Assert.True(HirNamePath.parse("unknown").is_unknown_type);
    }

    [Fact]
    public void HirTypeRef_ShouldExposeStructuredTypePredicates()
    {
        Assert.True(HirTypeRef.tuple([HirTypeRef.i32()]).is_tuple_type);
        Assert.True(HirTypeRef.unknown().is_unknown_type);
        Assert.True(HirTypeRef.named("std::collection::Array", [HirTypeRef.i32()]).is_array_type);
    }
}
