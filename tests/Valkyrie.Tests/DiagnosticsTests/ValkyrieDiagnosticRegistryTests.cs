using Nyar.Language.Valkyrie.Diagnostics;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using Xunit;
using TextSpan = Std.Text.TextSpan;

namespace Valkyrie.Tests.DiagnosticsTests;

/// <summary>
///     验证 <see cref="ValkyrieDiagnosticRegistry" /> 的规则注册、查询与展示码生成。
/// </summary>
public class ValkyrieDiagnosticRegistryTests
{
    [Fact]
    public void try_get_by_rule_name_should_return_descriptor_for_known_rule()
    {
        var found = ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT,
            out var descriptor);

        Assert.True(found);
        Assert.Equal(5, descriptor.stable_code);
        Assert.Equal(DiagnosticSeverity.error, descriptor.default_severity);
        Assert.Equal(ValkyrieDiagnosticSystem.check_core, descriptor.system);
        Assert.Equal("VALKYRIE_IMPORT_ALIAS_CONFLICT", descriptor.rule_name);
    }

    [Fact]
    public void try_get_by_rule_name_should_return_false_for_unknown_rule()
    {
        var found = ValkyrieDiagnosticRegistry.try_get_by_rule_name("VALKYRIE_NON_EXISTENT", out _);

        Assert.False(found);
    }

    [Fact]
    public void try_get_by_stable_code_should_return_descriptor_for_known_code()
    {
        var found = ValkyrieDiagnosticRegistry.try_get_by_stable_code(101, out var descriptor);

        Assert.True(found);
        Assert.Equal(ValkyrieRuleNames.DUPLICATE_USING, descriptor.rule_name);
        Assert.Equal(DiagnosticSeverity.warning, descriptor.default_severity);
        Assert.Equal(ValkyrieDiagnosticSystem.lint_core, descriptor.system);
    }

    [Fact]
    public void try_get_by_stable_code_should_return_false_for_unknown_code()
    {
        var found = ValkyrieDiagnosticRegistry.try_get_by_stable_code(9999, out _);

        Assert.False(found);
    }

    [Fact]
    public void all_should_be_sorted_by_stable_code_and_cover_expected_ranges()
    {
        var descriptors = ValkyrieDiagnosticRegistry.all;

        Assert.True(descriptors.Count >= 28);

        for (var i = 1; i < descriptors.Count; i++)
        {
            Assert.True(
                descriptors[i - 1].stable_code < descriptors[i].stable_code,
                $"descriptors at {i - 1} ({descriptors[i - 1].stable_code}) should be less than at {i} ({descriptors[i].stable_code})");
        }
    }

    [Fact]
    public void all_rule_names_should_use_VALKYRIE_prefix()
    {
        foreach (var descriptor in ValkyrieDiagnosticRegistry.all)
        {
            Assert.StartsWith("VALKYRIE_", descriptor.rule_name);
        }
    }

    [Theory]
    [InlineData(ValkyrieRuleNames.IMPORT_NOT_FOUND, 1, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core)]
    [InlineData(ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT, 5, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core)]
    [InlineData(ValkyrieRuleNames.REEXPORT_CYCLE, 11, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core)]
    [InlineData(ValkyrieRuleNames.DUPLICATE_USING, 101, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core)]
    [InlineData(ValkyrieRuleNames.UNUSED_USING, 103, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_core)]
    [InlineData(ValkyrieRuleNames.NAMESPACE_PRIMARY_SCOPE_CONFLICT, 201, DiagnosticSeverity.error, ValkyrieDiagnosticSystem.check_core)]
    [InlineData(ValkyrieRuleNames.IMPORT_ORDER_STYLE, 401, DiagnosticSeverity.warning, ValkyrieDiagnosticSystem.lint_style)]
    public void specific_descriptors_should_match_expected_metadata(
        string ruleName,
        int stableCode,
        DiagnosticSeverity severity,
        ValkyrieDiagnosticSystem system)
    {
        var found = ValkyrieDiagnosticRegistry.try_get_by_rule_name(ruleName, out var descriptor);

        Assert.True(found);
        Assert.Equal(stableCode, descriptor.stable_code);
        Assert.Equal(severity, descriptor.default_severity);
        Assert.Equal(system, descriptor.system);
    }

    [Fact]
    public void create_display_code_should_map_error_to_E_prefix()
    {
        ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT,
            out var descriptor);

        var code = ValkyrieDiagnosticRegistry.create_display_code(descriptor, DiagnosticSeverity.error);

        Assert.Equal("E0005", code);
    }

    [Fact]
    public void create_display_code_should_map_warning_to_W_prefix()
    {
        ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.DUPLICATE_USING,
            out var descriptor);

        var code = ValkyrieDiagnosticRegistry.create_display_code(descriptor, DiagnosticSeverity.warning);

        Assert.Equal("W0101", code);
    }

    [Fact]
    public void create_display_code_should_keep_stable_code_across_severity_changes()
    {
        ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT,
            out var descriptor);

        var asError = ValkyrieDiagnosticRegistry.create_display_code(descriptor, DiagnosticSeverity.error);
        var asWarning = ValkyrieDiagnosticRegistry.create_display_code(descriptor, DiagnosticSeverity.warning);

        Assert.Equal("E0005", asError);
        Assert.Equal("W0005", asWarning);
    }

    [Fact]
    public void create_display_code_should_respect_digits_option()
    {
        ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.IMPORT_NOT_FOUND,
            out var descriptor);

        var code = ValkyrieDiagnosticRegistry.create_display_code(descriptor, DiagnosticSeverity.error, 6);

        Assert.Equal("E000001", code);
    }

    [Fact]
    public void create_diagnostic_code_should_produce_composite_format()
    {
        ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT,
            out var descriptor);

        var code = ValkyrieDiagnosticRegistry.create_diagnostic_code(descriptor, DiagnosticSeverity.error);

        Assert.Equal("E0005 VALKYRIE_IMPORT_ALIAS_CONFLICT", code);
    }

    [Fact]
    public void create_diagnostic_code_should_switch_prefix_on_demotion()
    {
        ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT,
            out var descriptor);

        var asError = ValkyrieDiagnosticRegistry.create_diagnostic_code(descriptor, DiagnosticSeverity.error);
        var asWarning = ValkyrieDiagnosticRegistry.create_diagnostic_code(descriptor, DiagnosticSeverity.warning);

        Assert.Equal("E0005 VALKYRIE_IMPORT_ALIAS_CONFLICT", asError);
        Assert.Equal("W0005 VALKYRIE_IMPORT_ALIAS_CONFLICT", asWarning);
    }

    [Fact]
    public void get_default_rules_for_check_core_should_only_include_check_core_rules()
    {
        var rules = ValkyrieDiagnosticRegistry.get_default_rules(ValkyrieDiagnosticSystem.check_core);

        Assert.All(rules, descriptor => Assert.Equal(ValkyrieDiagnosticSystem.check_core, descriptor.system));
        Assert.Contains(rules, d => d.rule_name == ValkyrieRuleNames.IMPORT_NOT_FOUND);
        Assert.Contains(rules, d => d.rule_name == ValkyrieRuleNames.NAMESPACE_PRIMARY_CONFLICT);
        Assert.DoesNotContain(rules, d => d.rule_name == ValkyrieRuleNames.DUPLICATE_USING);
        Assert.DoesNotContain(rules, d => d.rule_name == ValkyrieRuleNames.IMPORT_ORDER_STYLE);
    }

    [Fact]
    public void get_default_rules_for_lint_core_should_include_check_core_and_lint_core()
    {
        var rules = ValkyrieDiagnosticRegistry.get_default_rules(ValkyrieDiagnosticSystem.lint_core);

        Assert.Contains(rules, d => d.rule_name == ValkyrieRuleNames.IMPORT_NOT_FOUND);
        Assert.Contains(rules, d => d.rule_name == ValkyrieRuleNames.DUPLICATE_USING);
        Assert.DoesNotContain(rules, d => d.rule_name == ValkyrieRuleNames.IMPORT_ORDER_STYLE);
    }

    [Fact]
    public void get_default_rules_for_lint_style_should_only_include_lint_style()
    {
        var rules = ValkyrieDiagnosticRegistry.get_default_rules(ValkyrieDiagnosticSystem.lint_style);

        Assert.All(rules, descriptor => Assert.Equal(ValkyrieDiagnosticSystem.lint_style, descriptor.system));
        Assert.Contains(rules, d => d.rule_name == ValkyrieRuleNames.IMPORT_ORDER_STYLE);
        Assert.DoesNotContain(rules, d => d.rule_name == ValkyrieRuleNames.IMPORT_NOT_FOUND);
    }

    [Fact]
    public void create_diagnostic_should_use_stable_code_for_known_rule()
    {
        var span = new TextSpan(0, 10);
        var diagnostic = ValkyrieDiagnosticRegistry.create_diagnostic(
            ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT,
            span,
            DiagnosticSeverity.error,
            "别名冲突");

        Assert.Equal(DiagnosticSeverity.error, diagnostic.severity);
        Assert.Equal(5, diagnostic.code);
    }

    [Fact]
    public void create_diagnostic_should_use_null_for_unknown_rule()
    {
        var span = new TextSpan(0, 10);
        var diagnostic = ValkyrieDiagnosticRegistry.create_diagnostic(
            "VALKYRIE_UNKNOWN_RULE",
            span,
            DiagnosticSeverity.warning,
            "未知规则");

        Assert.Null(diagnostic.code);
    }

    [Fact]
    public void format_stable_code_should_zero_pad()
    {
        ValkyrieDiagnosticRegistry.try_get_by_rule_name(
            ValkyrieRuleNames.IMPORT_NOT_FOUND,
            out var descriptor);

        Assert.Equal("0001", descriptor.format_stable_code());
        Assert.Equal("000001", descriptor.format_stable_code(6));
    }
}
