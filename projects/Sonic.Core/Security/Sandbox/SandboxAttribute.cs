using System;

namespace Core.Security.Sandbox;

/// <summary>
///     Sandbox 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SandboxAttribute : Attribute
{
    /// <summary>
    ///     沙箱策略
    /// </summary>
    public SandboxPolicy policy { get; set; } = SandboxPolicy.deny_all;
}