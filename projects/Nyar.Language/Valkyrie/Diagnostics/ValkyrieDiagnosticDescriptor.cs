using Std.Data.Text.Diagnostics;

namespace Nyar.Language.Valkyrie.Diagnostics;

/// <summary>
///     Valkyrie 诊断归属系统，决定一条规则在 <c>check</c> / <c>lint</c> 工具面中的可见性。
/// </summary>
public enum ValkyrieDiagnosticSystem
{
    /// <summary>
    ///     影响正确性、公开面稳定性、名称解析确定性的核心规则，默认进入 <c>check</c>。
    /// </summary>
    check_core,

    /// <summary>
    ///     复用 <see cref="check_core" /> 的语义域，并额外启用冗余、维护性、项目约定类规则。
    /// </summary>
    lint_core,

    /// <summary>
    ///     顺序、命名、复杂度、聚合约定、简化建议等风格规则，仅属于 <c>lint</c>。
    /// </summary>
    lint_style
}

/// <summary>
///     描述一条 Valkyrie 诊断规则的稳定元数据。
/// </summary>
/// <param name="rule_name">规则符号名，统一前缀 <c>VALKYRIE_</c>。</param>
/// <param name="stable_code">稳定四位数字编号，与严重级别解耦。</param>
/// <param name="default_severity">规则默认严重级别。</param>
/// <param name="floor_severity">规则最低严重级别。用户配置若低于此值将被忽略，以 floor 为准。</param>
/// <param name="system">规则归属系统，决定 <c>check</c> / <c>lint</c> 规则面归属。</param>
/// <param name="title">中文标题，用于文档与展示。</param>
/// <param name="message_template">默认消息模板，可由 validator 提供插值参数覆盖。</param>
public readonly record struct ValkyrieDiagnosticDescriptor(
    string rule_name,
    int stable_code,
    DiagnosticSeverity default_severity,
    DiagnosticSeverity floor_severity,
    ValkyrieDiagnosticSystem system,
    string title,
    string message_template)
{
    /// <summary>
    ///     返回四位数字编号的零填充字符串形式，例如 <c>0005</c>。
    /// </summary>
    public string format_stable_code(int digits = 4)
    {
        return stable_code.ToString($"D{System.Math.Max(digits, 1)}");
    }
}
