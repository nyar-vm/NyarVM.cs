namespace Nyar.IR.Rewrite;

/// <summary>
///     标记一个方法为额外条件检查方法，与 <see cref="RewriteGuardAttribute" /> 配合使用
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RewriteConditionAttribute : Attribute
{
    /// <summary>
    ///     对应的模式方法名
    /// </summary>
    public string pattern { get; init; } = string.Empty;
}