using Oak.Python.AST;

namespace Oak.Python.Tests;

public class PythonParserTests : PythonTestBase
{
    [Fact]
    public void EmptyModule()
    {
        var (module, diagnostics) = ParseWithTimeout("");
        Assert.False(diagnostics.HasErrors);
        Assert.Empty(module.Body);
    }

    [Fact]
    public void SimpleAssignment()
    {
        var (module, diagnostics) = ParseWithTimeout("x = 42");
        Assert.False(diagnostics.HasErrors);
        var assign = Assert.IsType<PyAssign>(Assert.Single(module.Body));
        Assert.IsType<PyIdentifier>(assign.Target);
        Assert.IsType<PyLiteral>(assign.Value);
    }

    [Fact]
    public void FunctionDef()
    {
        var source = """
            def hello():
                pass
            """;
        var (module, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var func = Assert.IsType<PyFunctionDef>(Assert.Single(module.Body));
        Assert.Equal("hello", func.Name);
        Assert.Empty(func.Parameters);
    }

    [Fact]
    public void IfStatement()
    {
        var source = """
            if True:
                pass
            """;
        var (module, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var ifStmt = Assert.IsType<PyIf>(Assert.Single(module.Body));
        Assert.Null(ifStmt.ElseBody);
    }

    [Fact]
    public void ImportStatement()
    {
        var (module, diagnostics) = ParseWithTimeout("import os");
        Assert.False(diagnostics.HasErrors);
        var import = Assert.IsType<PyImport>(Assert.Single(module.Body));
        Assert.Single(import.Items);
        Assert.Equal("os", import.Items[0].Name);
    }

    [Fact]
    public void BinaryExpression()
    {
        var (module, diagnostics) = ParseWithTimeout("x = 1 + 2");
        Assert.False(diagnostics.HasErrors);
        var assign = Assert.IsType<PyAssign>(Assert.Single(module.Body));
        var binOp = Assert.IsType<PyBinaryOp>(assign.Value);
        Assert.Equal("+", binOp.Operator);
    }

    [Fact]
    public void MultipleStatements()
    {
        var source = """
            x = 1
            y = 2
            z = x + y
            """;
        var (module, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        Assert.Equal(3, module.Body.Count);
    }

    [Fact]
    public void WhileStatement()
    {
        var source = """
            while True:
                pass
            """;
        var (module, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var whileStmt = Assert.IsType<PyWhile>(Assert.Single(module.Body));
        Assert.NotNull(whileStmt.Condition);
        Assert.NotEmpty(whileStmt.Body);
    }

    [Fact]
    public void ForStatement()
    {
        var source = """
            for x in items:
                pass
            """;
        var (module, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var forStmt = Assert.IsType<PyFor>(Assert.Single(module.Body));
        Assert.Equal("x", forStmt.Iterator);
    }

    [Fact]
    public void ClassDefinition()
    {
        var source = """
            class Foo:
                pass
            """;
        var (module, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var classDef = Assert.IsType<PyClassDef>(Assert.Single(module.Body));
        Assert.Equal("Foo", classDef.Name);
    }
}
