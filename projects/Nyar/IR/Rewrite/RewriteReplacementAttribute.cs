namespace Nyar.IR.Rewrite;

/// <summary>
///     标记一个方法为替换方法，与对应的 <see cref="RewriteRuleAttribute" /> 方法配对
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RewriteReplacementAttribute : Attribute
{
    /// <summary>
    ///     对应的模式方法名
    /// </summary>
    public string pattern { get; init; } = string.Empty;
}