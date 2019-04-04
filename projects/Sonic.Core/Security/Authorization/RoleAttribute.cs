using System;

namespace Core.Security.Authorization;

/// <summary>
///     Role 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RoleAttribute : Attribute
{
    /// <summary>
    ///     角色名称
    /// </summary>
    public string? name { get; set; }
}