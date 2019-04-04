using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Diagnostics;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Language.Valkyrie.TypeChecker;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie.Semantic;
using ValkyrieTypeChecker = Nyar.Language.Valkyrie.TypeChecker.TypeChecker;
using Xunit;

namespace Valkyrie.Tests.DiagnosticsTests;

/// <summary>
///     验证声明期 validator 通过 <c>ValkyrieSemanticBridge</c> 发射的复合 code 与严重级别。
/// </summary>
public sealed class ValkyrieDeclarationValidatorTests
{
    [Fact]
    public void duplicate_using_should_emit_composite_code()
    {
        var unit = parse_unit(
            """
            namespace app;

            using std.iterator.{Iterator};
            using std.iterator.{Iterator};
            """,
            "_.v");

        var model = build_model(unit);

        Assert.Contains(model.diagnostics, d =>
            d.code == ValkyrieDiagnosticRegistry.create_diagnostic_code(
                lookup(ValkyrieRuleNames.DUPLICATE_USING), DiagnosticSeverity.warning));
        Assert.Contains(model.diagnostics, d => d.level == DiagnosticSeverity.warning);
    }

    [Fact]
    public void import_alias_conflict_should_emit_composite_error_code()
    {
        var unit = parse_unit(
            """
            namespace app;

            using std.iterator.Iterator as Iter;
            using std.collection.ArrayList as Iter;
            """,
            "_.v");

        var model = build_model(unit);

        Assert.Contains(model.diagnostics, d =>
            d.code == ValkyrieDiagnosticRegistry.create_diagnostic_code(
                lookup(ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT), DiagnosticSeverity.error));
    }

    [Fact]
    public void import_duplicate_selection_should_emit_composite_error_code()
    {
        var unit = parse_unit(
            """
            namespace app;

            using std.iterator.{Iterator, Iterator};
            """,
            "_.v");

        var model = build_model(unit);

        Assert.Contains(model.diagnostics, d =>
            d.code == ValkyrieDiagnosticRegistry.create_diagnostic_code(
                lookup(ValkyrieRuleNames.IMPORT_DUPLICATE_SELECTION), DiagnosticSeverity.error));
    }

    [Fact]
    public void redundant_reexport_should_emit_composite_warning_code()
    {
        var unit = parse_unit(
            """
            namespace! std.iterator;

            using! std.iterator.{Iterator};

            trait Iterator {
            }
            """,
            "_.v");

        var model = build_model(unit);

        Assert.Contains(model.diagnostics, d =>
            d.code == ValkyrieDiagnosticRegistry.create_diagnostic_code(
                lookup(ValkyrieRuleNames.REDUNDANT_REEXPORT), DiagnosticSeverity.warning));
    }

    [Fact]
    public void reexport_outside_primary_entry_should_emit_warning()
    {
        var unit = parse_unit(
            """
            namespace! std.iterator;

            using! std.other.{Helper};

            trait Iterator {
            }
            """,
            "iterator_impl.v");

        var model = build_model(unit);

        Assert.Contains(model.diagnostics, d =>
            d.code == ValkyrieDiagnosticRegistry.create_diagnostic_code(
                lookup(ValkyrieRuleNames.REEXPORT_OUTSIDE_PRIMARY_ENTRY), DiagnosticSeverity.warning));
    }

    [Fact]
    public void reexport_inside_primary_entry_should_not_warn_about_location()
    {
        var unit = parse_unit(
            """
            namespace! std.iterator;

            using! std.other.{Helper};
            """,
            "_.v");

        var model = build_model(unit);

        Assert.DoesNotContain(model.diagnostics, d =>
            d.code != null && d.code.Contains(ValkyrieRuleNames.REEXPORT_OUTSIDE_PRIMARY_ENTRY));
    }

    [Fact]
    public void namespace_primary_duplicate_in_file_should_emit_error()
    {
        var unit = parse_unit(
            """
            namespace! app.core;
            namespace! app.core;
            """,
            "_.v");

        var model = build_model(unit);

        Assert.Contains(model.diagnostics, d =>
            d.code == ValkyrieDiagnosticRegistry.create_diagnostic_code(
                lookup(ValkyrieRuleNames.NAMESPACE_PRIMARY_DUPLICATE_IN_FILE), DiagnosticSeverity.error));
    }

    private static ValkyrieDiagnosticDescriptor lookup(string ruleName)
    {
        Assert.True(ValkyrieDiagnosticRegistry.try_get_by_rule_name(ruleName, out var descriptor));
        return descriptor;
    }

    private static CompilationUnit parse_unit(string source, string filePath)
    {
        var analyzer = new SourceAnalyzer();
        var result = analyzer.parse(source, filePath);
        Assert.True(result.success,
            string.Join(Environment.NewLine, result.diagnostics.Select(d => d.message)));
        return Assert.IsType<CompilationUnit>(result.value);
    }

    private static SemanticModel build_model(params CompilationUnit[] units)
    {
        var bridge = new ValkyrieSemanticBridge();
        return bridge.build_semantic_model(
            new TypeCheckResult([]),
            units,
            "workspace");
    }
}
