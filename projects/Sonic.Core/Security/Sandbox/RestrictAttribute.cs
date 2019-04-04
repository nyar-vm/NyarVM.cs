using System;

namespace Core.Security.Sandbox;

/// <summary>
///     Restrict 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RestrictAttribute : Attribute
{
    /// <summary>
    ///     资源类型
    /// </summary>
    public ResourceKind resource { get; set; } = ResourceKind.none;


    /// <summary>
    ///     访问权限
    /// </summary>
    public AccessPermission permission { get; set; } = AccessPermission.none;
}