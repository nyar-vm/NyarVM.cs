using System;

namespace Core.Security.Authorization;

/// <summary>
///     标记需要授权的类或方法，指定允许的角色
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    ///     初始化授权属性
    /// </summary>
    /// <param name="roles">允许访问的角色</param>
    public AuthorizeAttribute(params string[] roles)
    {
        this.roles = roles;
    }

    /// <summary>
    ///     允许访问的角色列表
    /// </summary>
    public string[] roles { get; }
}