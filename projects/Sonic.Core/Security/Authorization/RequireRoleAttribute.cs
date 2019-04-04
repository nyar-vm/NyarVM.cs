using System;

namespace Core.Security.Authorization;

/// <summary>
///     要求调用者具备指定角色才能访问
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute : Attribute
{
    /// <summary>
    ///     初始化角色要求特性
    /// </summary>
    /// <param name="roles">允许访问的角色列表</param>
    public RequireRoleAttribute(params string[] roles)
    {
        this.roles = roles;
    }

    /// <summary>
    ///     允许访问的角色列表
    /// </summary>
    public string[] roles { get; }
}