namespace Std.Data.Text.CSharp.Oak.CSharp.Tests.ParserTests;

public class CsParserTests
{
    private readonly CsPipeline _pipeline = new();

    [Fact]
    public void Parse_EmptySource_ShouldReturnCompilationUnit()
    {
        var result = _pipeline.Parse("");

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.Empty(unit.Declarations);
    }

    [Fact]
    public void Parse_UsingDirective_ShouldReturnUsing()
    {
        var source = "using System;";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.Single(unit.Usings);
        Assert.Equal("System", unit.Usings[0].Namespace);
    }

    [Fact]
    public void Parse_NamespaceWithClass_ShouldParseNamespace()
    {
        var source = "namespace MyApp { class Program { } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_ClassDecl_ShouldParseClass()
    {
        var source = "public class Calculator { }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_ClassWithMethod_ShouldParseMethod()
    {
        var source = "class Foo { int Add(int a, int b) { return a + b; } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_EnumDecl_ShouldParseEnum()
    {
        var source = "enum Color { Red, Green, Blue }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_StructDecl_ShouldParseStruct()
    {
        var source = "struct Point { int X; int Y; }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_InterfaceDecl_ShouldParseInterface()
    {
        var source = "interface IShape { int Area(); }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_RecordDecl_ShouldParseRecord()
    {
        var source = "record Person(string Name, int Age) { }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_PropertyDecl_ShouldParseProperty()
    {
        var source = "class Foo { int Value { get; set; } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }

    [Fact]
    public void Parse_IfStatement_ShouldParseIf()
    {
        var source = "class C { void M() { if (true) { return; } } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_ForEachStatement_ShouldParseForEach()
    {
        var source = "class C { void M() { foreach (var item in items) { } } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_TryCatchStatement_ShouldParseTryCatch()
    {
        var source = "class C { void M() { try { } catch (Exception ex) { } } }";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_DelegateDecl_ShouldParseDelegate()
    {
        var source = "delegate int Transformer(int x);";
        var result = _pipeline.Parse(source);

        Assert.NotNull(result);
        var unit = Assert.IsType<CsCompilationUnit>(result);
        Assert.NotEmpty(unit.Declarations);
    }
}
