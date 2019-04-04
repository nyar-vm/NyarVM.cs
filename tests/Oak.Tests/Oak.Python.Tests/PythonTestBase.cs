using Oak.Diagnostics;
using Oak.Python.AST;
using Oak.Python.Lexer;
using Oak.Python.Parser;
using Oak.Testing;

namespace Oak.Python.Tests;

public abstract class PythonTestBase : TestBase
{
    protected (PyModule Module, DiagnosticSink Diagnostics) ParseWithTimeout(string source)
    {
        PyModule? module = null;
        var diagnostics = new DiagnosticSink();

        ExecuteWithTimeout(() =>
        {
            var lexer = new PythonLexer();
            var tokens = lexer.Tokenize(source);
            var parser = new PythonParser(diagnostics);
            var result = parser.Parse(tokens);
            module = Assert.IsType<PyModule>(result);
        }, "Python 解析器");

        return (module!, diagnostics);
    }
}
