using System;

namespace Core.Net.Signal;

/// <summary>
///     Push 属性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PushAttribute : Attribute
{
    /// <summary>
    ///     推送主题
    /// </summary>
    public string? topic { get; set; }
}