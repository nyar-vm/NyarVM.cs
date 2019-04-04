namespace Nyar.IR.Rewrite;

/// <summary>
///     标记一个方法为守卫方法，在重写规则应用前检查条件
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RewriteGuardAttribute : Attribute
{
    /// <summary>
    ///     对应的模式方法名
    /// </summary>
    public string pattern { get; init; } = string.Empty;
}