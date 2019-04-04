using Std.Data.Text.Valkyrie.Semantic;
using ValkyrieTypeChecker = Nyar.Language.Valkyrie.TypeChecker.TypeChecker;

namespace Valkyrie.Tests.SemanticTests;

public sealed class NamespaceSemanticTests
{
    [Fact]
    public void BuildSemanticModel_MultipleFileScopedNamespaces_ShouldCreateQualifiedScopes()
    {
        var analyzer = new SourceAnalyzer();
        var printUnit = assert_parse_success(analyzer.parse(
            """
            namespace std.io;

            micro print() -> Unit {
            }
            """,
            "std.io.v"));
        var appUnit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            micro main() -> Unit {
            }
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [printUnit, appUnit],
            "workspace");

        Assert.NotNull(model.resolve_symbol("std::io::print"));
        Assert.NotNull(model.resolve_symbol("app::main"));
    }

    [Fact]
    public void BuildSemanticModel_ReexportCurrentNamespace_ShouldReportWarning()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace! std.iterator;

            using! std.iterator.{Iterator};

            trait Iterator {
            }
            """,
            "std.iterator._.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_REDUNDANT_REEXPORT");
        Assert.Contains(model.diagnostics, diagnostic => diagnostic.level == DiagnosticSeverity.warning);
    }

    [Fact]
    public void BuildSemanticModel_DuplicateUsing_ShouldReportWarning()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            using std.iterator.{Iterator};
            using std.iterator.{Iterator};
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_DUPLICATE_USING");
        Assert.Contains(model.diagnostics, diagnostic => diagnostic.level == DiagnosticSeverity.warning);
    }

    [Fact]
    public void BuildSemanticModel_ImportAliasConflict_ShouldReportError()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            using std.iterator.Iterator as Iter;
            using std.collection.ArrayList as Iter;
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_IMPORT_ALIAS_CONFLICT");
        Assert.Contains(model.diagnostics, diagnostic => diagnostic.level == DiagnosticSeverity.error);
    }

    private static CompilationUnit assert_parse_success(ParseResult<CompilationUnit> result)
    {
        Assert.True(result.success, string.Join(Environment.NewLine, result.diagnostics.Select(d => d.message)));
        return Assert.IsType<CompilationUnit>(result.value);
    }
}
