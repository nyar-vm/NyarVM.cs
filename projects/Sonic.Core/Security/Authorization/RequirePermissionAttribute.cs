using System;

namespace Core.Security.Authorization;

/// <summary>
///     RequirePermission 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute : Attribute
{
    /// <summary>
    ///     所需权限
    /// </summary>
    public string? permission { get; set; }
}