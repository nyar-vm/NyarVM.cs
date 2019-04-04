using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;

namespace Nyar.Tests.Compiler;

/// <summary>
///     提供增量缓存测试所需的临时工作区与示例语义数据。
/// </summary>
internal sealed class IncrementalCacheTestContext : IDisposable
{
    private readonly string _workspaceDir;

    public IncrementalCacheTestContext()
    {
        _workspaceDir = Path.Combine(Path.GetTempPath(), $"nyar_cache_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspaceDir);
    }

    public string workspace_dir => _workspaceDir;

    public void Dispose()
    {
        if (Directory.Exists(_workspaceDir))
        {
            Directory.Delete(_workspaceDir, true);
        }
    }

    public NyarDatabaseCompilationCache create_cache()
    {
        return new NyarDatabaseCompilationCache(_workspaceDir);
    }

    public string write_source_file(string fileName, string content)
    {
        var filePath = Path.Combine(_workspaceDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    public string create_semantic_cache_source()
    {
        return write_source_file(
            "semantic_cache_hit.v",
            """
            namespace simple_cli;

            micro helper() -> i32 {
                42
            }

            [main]
            micro main() -> i32 {
                helper()
            }
            """);
    }

    public SemanticModel create_demo_semantic_model()
    {
        var globalScope = new Scope("global");
        var moduleScope = globalScope.get_or_create_child_scope("demo");
        var symbolTable = new SymbolTable(globalScope);

        var i32Type = new PrimitiveType("i32");
        var fieldSymbol = create_symbol(
            "value",
            SymbolKind.field,
            i32Type,
            new TextSpan(5, 5),
            new SourceSpan("test.v", 1, 6, 1, 10),
            isReadOnly: true);
        moduleScope.define(fieldSymbol);

        var rowType = new RowType([fieldSymbol], false, "Point");
        var typeVariable = new TypeVariable("T");
        typeVariable.bind(rowType);
        var functionType = new FunctionType([i32Type, typeVariable], rowType);
        var functionSymbol = create_symbol(
            "makePoint",
            SymbolKind.function,
            functionType,
            new TextSpan(20, 9),
            new SourceSpan("test.v", 2, 1, 2, 9),
            isStatic: true);
        moduleScope.define(functionSymbol);

        var namedType = new NamedType("Point", "struct", null, [i32Type], [fieldSymbol]);
        var typeSymbol = create_symbol(
            "Point",
            SymbolKind.type_alias,
            namedType,
            new TextSpan(0, 5),
            new SourceSpan("test.v", 1, 1, 1, 5));
        moduleScope.define(typeSymbol);

        symbolTable.add_reference(functionSymbol, fieldSymbol);

        var semanticModel = new SemanticModel("test.v", symbolTable);
        semanticModel.bind_symbol(1, functionSymbol);
        semanticModel.bind_symbol(2, typeSymbol);
        semanticModel.bind_type(1, functionType);
        semanticModel.bind_type(2, namedType);
        semanticModel.add_diagnostic(new SemanticDiagnostic(
            DiagnosticSeverity.warning,
            "测试诊断",
            new TextSpan(20, 4),
            "NYAR0001",
            new SourceSpan("test.v", 2, 1, 2, 4),
            "test.v"));
        return semanticModel;
    }

    private static Symbol create_symbol(
        string name,
        SymbolKind kind,
        IType type,
        TextSpan definitionSpan,
        SourceSpan definitionSourceSpan,
        bool isStatic = false,
        bool isReadOnly = false)
    {
        return new Symbol(
            name,
            kind,
            SymbolAccessibility.@public,
            type,
            null,
            isStatic,
            isReadOnly,
            false,
            false,
            definitionSpan,
            definitionSourceSpan,
            "test.v");
    }
}
