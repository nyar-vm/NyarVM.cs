using System;

namespace Core.Security.Authorization;

/// <summary>
///     Policy 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PolicyAttribute : Attribute
{
    /// <summary>
    ///     策略名称
    /// </summary>
    public string? name { get; set; }
}