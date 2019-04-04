using Nyar.Analyzer.Semantic;

namespace Nyar.Tests.Semantic;

public class GenericInferenceTests
{
    [Fact]
    public void SingleTypeVariable_IdentityFunction()
    {
        // fn<T>(x: T) -> T
        var paramTypes = new List<IType> { new GenericType("T", []) };
        var argTypes = new List<IType> { new PrimitiveType("i32") };
        var genericParams = new List<string> { "T" };

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.True(result.is_success);
        Assert.True(result.substitutions.ContainsKey("T"));
        Assert.IsType<PrimitiveType>(result.substitutions["T"]);
        Assert.Equal("i32", ((PrimitiveType)result.substitutions["T"]).name);
    }

    [Fact]
    public void SingleTypeVariable_StringArgument()
    {
        // fn<T>(x: T) -> T
        var paramTypes = new List<IType> { new GenericType("T", []) };
        var argTypes = new List<IType> { new PrimitiveType("string") };
        var genericParams = new List<string> { "T" };

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.True(result.is_success);
        Assert.Equal("string", ((PrimitiveType)result.substitutions["T"]).name);
    }

    [Fact]
    public void ListTypeVariable_InferElementType()
    {
        // fn<T>(x: list<T>) -> T
        var paramTypes = new List<IType>
        {
            new GenericType("list", [new GenericType("T", [])])
        };
        var argTypes = new List<IType>
        {
            new GenericType("list", [new PrimitiveType("f64")])
        };
        var genericParams = new List<string> { "T" };

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.True(result.is_success);
        Assert.True(result.substitutions.ContainsKey("T"));
        Assert.Equal("f64", ((PrimitiveType)result.substitutions["T"]).name);
    }

    [Fact]
    public void MultipleTypeVariables_MapFunction()
    {
        // fn<K, V>(key: K, value: V, map: map<K, V>) -> V
        var paramTypes = new List<IType>
        {
            new GenericType("K", []),
            new GenericType("V", []),
            new GenericType("map",
            [
                new GenericType("K", []),
                new GenericType("V", [])
            ])
        };
        var argTypes = new List<IType>
        {
            new PrimitiveType("string"),
            new PrimitiveType("i32"),
            new GenericType("map",
            [
                new PrimitiveType("string"),
                new PrimitiveType("i32")
            ])
        };
        var genericParams = new List<string> { "K", "V" };

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.True(result.is_success);
        Assert.Equal("string", ((PrimitiveType)result.substitutions["K"]).name);
        Assert.Equal("i32", ((PrimitiveType)result.substitutions["V"]).name);
    }

    [Fact]
    public void ConflictingTypeVariable_ReturnsFailure()
    {
        // fn<T>(a: T, b: T) -> T — 需要 a 和 b 类型一致
        var paramTypes = new List<IType>
        {
            new GenericType("T", []),
            new GenericType("T", [])
        };
        var argTypes = new List<IType>
        {
            new PrimitiveType("i32"),
            new PrimitiveType("string")
        };
        var genericParams = new List<string> { "T" };

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.False(result.is_success);
    }

    [Fact]
    public void ConsistentTypeVariable_ReturnsSuccess()
    {
        // fn<T>(a: T, b: T) -> T — a 和 b 都是 i32
        var paramTypes = new List<IType>
        {
            new GenericType("T", []),
            new GenericType("T", [])
        };
        var argTypes = new List<IType>
        {
            new PrimitiveType("i32"),
            new PrimitiveType("i32")
        };
        var genericParams = new List<string> { "T" };

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.True(result.is_success);
        Assert.Equal("i32", ((PrimitiveType)result.substitutions["T"]).name);
    }

    [Fact]
    public void MismatchedParameterCount_ReturnsFailure()
    {
        var paramTypes = new List<IType> { new GenericType("T", []), new GenericType("U", []) };
        var argTypes = new List<IType> { new PrimitiveType("i32") };
        var genericParams = new List<string> { "T", "U" };

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.False(result.is_success);
    }

    [Fact]
    public void NoGenericParams_EmptySubstitutions()
    {
        var paramTypes = new List<IType> { new PrimitiveType("i32") };
        var argTypes = new List<IType> { new PrimitiveType("i32") };
        var genericParams = new List<string>();

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.True(result.is_success);
        Assert.Empty(result.substitutions);
    }

    [Fact]
    public void Apply_SubstitutesTypeVariableInReturnType()
    {
        // fn<T>(x: T, y: T) -> list<T>
        var paramTypes = new List<IType>
        {
            new GenericType("T", []),
            new GenericType("T", [])
        };
        var argTypes = new List<IType>
        {
            new PrimitiveType("f64"),
            new PrimitiveType("f64")
        };
        var genericParams = new List<string> { "T" };
        var returnType = new GenericType("list", [new GenericType("T", [])]);

        var result = GenericInferrer.infer(genericParams, paramTypes, argTypes);

        Assert.True(result.is_success);
        var resolved = result.apply(returnType);
        var resolvedList = Assert.IsType<GenericType>(resolved);
        Assert.Equal("list", resolvedList.name);
        Assert.Equal("f64", ((PrimitiveType)resolvedList.type_arguments[0]).name);
    }
}