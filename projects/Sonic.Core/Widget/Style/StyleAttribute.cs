using System;

namespace Core.Widget.Style;

/// <summary>
///     Style 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StyleAttribute : Attribute
{
    /// <summary>
    ///     样式键名
    /// </summary>
    public string? key { get; set; }
}