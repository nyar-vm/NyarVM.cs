namespace Std.App.Server.Attributes;

/// <summary>
///     要求控制器或 Action 经过 JWT 认证才能访问
///     可指定角色要求
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    ///     允许访问的角色列表，为空表示仅需登录
    /// </summary>
    public string[]? roles { get; init; }

    /// <summary>
    ///     JWT 认证 Scheme，默认 Bearer
    /// </summary>
    public string scheme { get; init; } = "Bearer";
}