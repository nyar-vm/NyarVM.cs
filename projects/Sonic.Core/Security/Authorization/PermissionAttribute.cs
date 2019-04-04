using System;

namespace Core.Security.Authorization;

/// <summary>
///     Permission 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermissionAttribute : Attribute
{
    /// <summary>
    ///     权限名称
    /// </summary>
    public string? name { get; set; }
}