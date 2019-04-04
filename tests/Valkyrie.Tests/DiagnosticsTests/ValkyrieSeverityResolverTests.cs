using Nyar.Language.Valkyrie.Diagnostics;
using Std.Data.Text.Diagnostics;
using Xunit;

namespace Valkyrie.Tests.DiagnosticsTests;

/// <summary>
///     验证 <see cref="ValkyrieSeverityResolver" /> 的默认级别、规则面归属、用户覆盖与 floor 机制。
/// </summary>
public class ValkyrieSeverityResolverTests
{
    private static ValkyrieDiagnosticDescriptor get_descriptor(string ruleName)
    {
        Assert.True(ValkyrieDiagnosticRegistry.try_get_by_rule_name(ruleName, out var descriptor));
        return descriptor;
    }

    [Fact]
    public void resolve_should_return_default_severity_without_overrides()
    {
        var descriptor = get_descriptor(ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT);

        var resolved = ValkyrieSeverityResolver.resolve(
            descriptor,
            ValkyrieDiagnosticSystem.check_core);

        Assert.Equal(DiagnosticSeverity.error, resolved);
    }

    [Fact]
    public void resolve_should_return_null_for_rule_not_visible_in_tool_system()
    {
        var lintStyleDescriptor = get_descriptor(ValkyrieRuleNames.IMPORT_ORDER_STYLE);

        var resolvedUnderCheck = ValkyrieSeverityResolver.resolve(
            lintStyleDescriptor,
            ValkyrieDiagnosticSystem.check_core);

        Assert.Null(resolvedUnderCheck);
    }

    [Fact]
    public void resolve_should_promote_warning_to_error_by_rule_name()
    {
        var descriptor = get_descriptor(ValkyrieRuleNames.DUPLICATE_USING);

        var workspaceOverrides = new ValkyrieSeverityOverrides();
        workspaceOverrides.set_by_rule_name(ValkyrieRuleNames.DUPLICATE_USING, DiagnosticSeverity.error);

        var resolved = ValkyrieSeverityResolver.resolve(
            descriptor,
            ValkyrieDiagnosticSystem.lint_core,
            workspaceOverrides);

        Assert.Equal(DiagnosticSeverity.error, resolved);
    }

    [Fact]
    public void resolve_should_clamp_warning_config_to_error_floor()
    {
        // 规则 floor = error，用户设置 warning → 被 clamp 到 error
        var descriptor = get_descriptor(ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT);
        Assert.Equal(DiagnosticSeverity.error, descriptor.floor_severity);

        var workspaceOverrides = new ValkyrieSeverityOverrides();
        workspaceOverrides.set_by_rule_name(ValkyrieRuleNames.IMPORT_ALIAS_CONFLICT, DiagnosticSeverity.warning);

        var resolved = ValkyrieSeverityResolver.resolve(
            descriptor,
            ValkyrieDiagnosticSystem.check_core,
            workspaceOverrides);

        // 不能降级到 warning 以下，floor=error
        Assert.Equal(DiagnosticSeverity.error, resolved);
    }

    [Fact]
    public void resolve_should_clamp_hint_config_to_warning_floor()
    {
        // 规则 floor = warning，用户设置 hint → 被 clamp 到 warning
        var descriptor = get_descriptor(ValkyrieRuleNames.DUPLICATE_USING);
        Assert.Equal(DiagnosticSeverity.warning, descriptor.floor_severity);

        var workspaceOverrides = new ValkyrieSeverityOverrides();
        workspaceOverrides.set_by_rule_name(ValkyrieRuleNames.DUPLICATE_USING, DiagnosticSeverity.hint);

        var resolved = ValkyrieSeverityResolver.resolve(
            descriptor,
            ValkyrieDiagnosticSystem.lint_core,
            workspaceOverrides);

        // 不能降级到 warning 以下，floor=warning
        Assert.Equal(DiagnosticSeverity.warning, resolved);
    }

    [Fact]
    public void resolve_should_promote_warning_to_error_by_stable_code()
    {
        var descriptor = get_descriptor(ValkyrieRuleNames.UNUSED_USING);

        var workspaceOverrides = new ValkyrieSeverityOverrides();
        workspaceOverrides.set_by_stable_code(103, DiagnosticSeverity.error);

        var resolved = ValkyrieSeverityResolver.resolve(
            descriptor,
            ValkyrieDiagnosticSystem.lint_core,
            workspaceOverrides);

        Assert.Equal(DiagnosticSeverity.error, resolved);
    }

    [Fact]
    public void resolve_project_override_should_win_over_workspace()
    {
        var descriptor = get_descriptor(ValkyrieRuleNames.DUPLICATE_USING);

        var workspace = new ValkyrieSeverityOverrides();
        workspace.set_by_rule_name(ValkyrieRuleNames.DUPLICATE_USING, DiagnosticSeverity.error);

        var project = new ValkyrieSeverityOverrides();
        project.set_by_rule_name(ValkyrieRuleNames.DUPLICATE_USING, DiagnosticSeverity.warning);

        var resolved = ValkyrieSeverityResolver.resolve(
            descriptor,
            ValkyrieDiagnosticSystem.lint_core,
            workspace,
            project);

        Assert.Equal(DiagnosticSeverity.warning, resolved);
    }

    [Fact]
    public void resolve_project_override_should_not_downgrade_below_floor()
    {
        // floor=error 的规则，项目配置 warning 也没用
        var descriptor = get_descriptor(ValkyrieRuleNames.IMPORT_NOT_FOUND);
        Assert.Equal(DiagnosticSeverity.error, descriptor.floor_severity);

        var project = new ValkyrieSeverityOverrides();
        project.set_by_rule_name(ValkyrieRuleNames.IMPORT_NOT_FOUND, DiagnosticSeverity.warning);

        var resolved = ValkyrieSeverityResolver.resolve(
            descriptor,
            ValkyrieDiagnosticSystem.check_core,
            null,
            project);

        // floor=error 阻止降级
        Assert.Equal(DiagnosticSeverity.error, resolved);
    }

    [Theory]
    [InlineData(ValkyrieDiagnosticSystem.check_core, ValkyrieDiagnosticSystem.check_core, true)]
    [InlineData(ValkyrieDiagnosticSystem.check_core, ValkyrieDiagnosticSystem.lint_core, true)]
    [InlineData(ValkyrieDiagnosticSystem.lint_core, ValkyrieDiagnosticSystem.lint_core, true)]
    [InlineData(ValkyrieDiagnosticSystem.lint_style, ValkyrieDiagnosticSystem.lint_style, true)]
    [InlineData(ValkyrieDiagnosticSystem.lint_core, ValkyrieDiagnosticSystem.check_core, false)]
    [InlineData(ValkyrieDiagnosticSystem.lint_style, ValkyrieDiagnosticSystem.check_core, false)]
    [InlineData(ValkyrieDiagnosticSystem.lint_style, ValkyrieDiagnosticSystem.lint_core, false)]
    public void is_rule_visible_in_system_should_model_superset_semantics(
        ValkyrieDiagnosticSystem ruleSystem,
        ValkyrieDiagnosticSystem toolSystem,
        bool expected)
    {
        var visible = ValkyrieSeverityResolver.is_rule_visible_in_system(ruleSystem, toolSystem);

        Assert.Equal(expected, visible);
    }
}
