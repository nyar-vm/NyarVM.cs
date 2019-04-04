using Std.Data.Text.Valkyrie.AST;

namespace Nyar.Lint;

/// <summary>
///     Lint 规则接口：检查 AST 节点并返回诊断信息
/// </summary>
public interface ILintRule
{
    /// <summary>
    ///     规则唯一标识
    /// </summary>
    string Code { get; }

    /// <summary>
    ///     规则描述
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     诊断严重级别
    /// </summary>
    LintSeverity Severity { get; }

    /// <summary>
    ///     检查 AST 节点，返回诊断信息
    /// </summary>
    IEnumerable<LintDiagnostic> Diagnose(ValkyrieNode node);
}