namespace Nyar.IR.Rewrite;

/// <summary>
///     标记一个方法为模式方法，与对应的 <see cref="RewriteReplacementAttribute" /> 方法配对生成重写规则
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RewriteRuleAttribute : Attribute
{
    /// <summary>
    ///     规则名称，用于调试和日志输出
    /// </summary>
    public string name { get; init; } = string.Empty;
}