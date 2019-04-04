using System.Collections.Generic;
using Std.Data.Text.Diagnostics;

namespace Nyar.Language.Valkyrie.Diagnostics;

/// <summary>
///     用户对诊断规则的严重级别覆盖配置。
///     用户配置低于规则 <c>floor_severity</c> 的值会被忽略，以 floor 为准。
/// </summary>
public sealed class ValkyrieSeverityOverrides
{
    private readonly Dictionary<string, DiagnosticSeverity> byRuleName = new(System.StringComparer.Ordinal);
    private readonly Dictionary<int, DiagnosticSeverity> byStableCode = new();

    /// <summary>
    ///     按规则名设置覆盖级别。
    /// </summary>
    public void set_by_rule_name(string ruleName, DiagnosticSeverity severity)
    {
        byRuleName[ruleName] = severity;
    }

    /// <summary>
    ///     按稳定编号设置覆盖级别。
    /// </summary>
    public void set_by_stable_code(int stableCode, DiagnosticSeverity severity)
    {
        byStableCode[stableCode] = severity;
    }

    /// <summary>
    ///     尝试获取 <paramref name="descriptor" /> 对应的覆盖级别。
    ///     优先按规则名匹配，其次按稳定编号匹配。
    /// </summary>
    public bool try_get(ValkyrieDiagnosticDescriptor descriptor, out DiagnosticSeverity severity)
    {
        if (byRuleName.TryGetValue(descriptor.rule_name, out severity))
        {
            return true;
        }

        return byStableCode.TryGetValue(descriptor.stable_code, out severity);
    }
}

/// <summary>
///     严重级别决议器。
///     <para>
///         决议逻辑：工具面过滤 → 用户配置覆盖（工作区 → 项目）→ 不可低于 floor。
///     </para>
/// </summary>
public static class ValkyrieSeverityResolver
{
    /// <summary>
    ///     计算规则在给定工具面与覆盖配置下的最终严重级别。
    /// </summary>
    /// <param name="descriptor">规则描述符。</param>
    /// <param name="toolSystem">当前工具面（<c>check_core</c> / <c>lint_core</c> / <c>lint_style</c>）。</param>
    /// <param name="workspaceOverrides">工作区级别覆盖，可选。</param>
    /// <param name="projectOverrides">项目级别覆盖，可选。</param>
    /// <returns>
    ///     规则生效的严重级别。如果规则在当前工具面下不可见，返回 <c>null</c>。
    /// </returns>
    public static DiagnosticSeverity? resolve(
        ValkyrieDiagnosticDescriptor descriptor,
        ValkyrieDiagnosticSystem toolSystem,
        ValkyrieSeverityOverrides? workspaceOverrides = null,
        ValkyrieSeverityOverrides? projectOverrides = null)
    {
        // 1. 工具面过滤：规则不属于当前工具面 → 不发射
        if (!is_rule_visible_in_system(descriptor.system, toolSystem))
        {
            return null;
        }

        // 2. 取默认级别
        var effective = descriptor.default_severity;

        // 3. 用户配置覆盖（后覆盖前：项目 > 工作区）
        if (workspaceOverrides?.try_get(descriptor, out var ws) == true)
        {
            effective = ws;
        }

        if (projectOverrides?.try_get(descriptor, out var proj) == true)
        {
            effective = proj;
        }

        // 4. 不可低于 floor：用户配置若低于 floor 则被忽略，以 floor 为准
        if (is_more_lenient_than(effective, descriptor.floor_severity))
        {
            effective = descriptor.floor_severity;
        }

        return effective;
    }

    /// <summary>
    ///     判断 <paramref name="ruleSystem" /> 在 <paramref name="toolSystem" /> 下是否可见。
    ///     <para>
    ///         <c>lint_core</c> 包含 <c>check_core</c> 的规则；
    ///         <c>lint_style</c> 仅含自身，不被 <c>check</c> 和 <c>lint_core</c> 包含。
    ///     </para>
    /// </summary>
    public static bool is_rule_visible_in_system(
        ValkyrieDiagnosticSystem ruleSystem,
        ValkyrieDiagnosticSystem toolSystem)
    {
        return ruleSystem == toolSystem ||
               (toolSystem == ValkyrieDiagnosticSystem.lint_core &&
                ruleSystem == ValkyrieDiagnosticSystem.check_core);
    }

    /// <summary>
    ///     判断 <paramref name="candidate" /> 是否比 <paramref name="floor" /> 更宽松（值更小表示更严格）。
    ///     <c>fatal(0) &gt; error(1) &gt; warning(2) &gt; info(3) &gt; hint(4) &gt; debug(5) &gt; trace(6)</c>
    /// </summary>
    private static bool is_more_lenient_than(DiagnosticSeverity candidate, DiagnosticSeverity floor)
    {
        return (int)candidate > (int)floor;
    }
}
