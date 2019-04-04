namespace Core.Security.Sandbox;

/// <summary>
///     SandboxPolicy 枚举
/// </summary>
public enum SandboxPolicy
{
    /// <summary>
    ///     默认拒绝所有
    /// </summary>
    deny_all,

    /// <summary>
    ///     默认允许所有
    /// </summary>
    allow_all,

    /// <summary>
    ///     自定义策略
    /// </summary>
    custom
}