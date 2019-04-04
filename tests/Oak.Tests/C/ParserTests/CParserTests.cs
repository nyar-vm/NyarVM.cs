namespace Oak.C.Tests.ParserTests;

public class CParserTests
{
    private readonly CPipeline _pipeline = new();

    [Fact]
    public void Parse_EmptySource_ShouldReturnTranslationUnit()
    {
        var result = _pipeline.Parse("");

        Assert.NotNull(result);
        var unit = Assert.IsType<CTranslationUnit>(result);
        Assert.Empty(unit.Declarations);
    }

    [Fact]
    public void Parse_FunctionDeclaration_ShouldReturnFunctionDef()
    {
        var source = "int main() { return 0; }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CTranslationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_VariableDeclaration_ShouldReturnVarDecl()
    {
        var source = "int x = 42;";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CTranslationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_StructDeclaration_ShouldParseStruct()
    {
        var source = "struct Point { int x; int y; };";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CTranslationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_IfStatement_ShouldParseIf()
    {
        var source = "int main() { if (1) { return 1; } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_WhileLoop_ShouldParseWhile()
    {
        var source = "int main() { while (1) { break; } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_ForLoop_ShouldParseFor()
    {
        var source = "int main() { for (int i = 0; i < 10; i++) { } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_FunctionWithParameters_ShouldParseParams()
    {
        var source = "int add(int a, int b) { return a + b; }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CTranslationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_VoidFunction_ShouldParseVoidReturn()
    {
        var source = "void hello() { }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CTranslationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_TypedefDeclaration_ShouldParseTypedef()
    {
        var source = "typedef int MyInt;";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
    }
}
