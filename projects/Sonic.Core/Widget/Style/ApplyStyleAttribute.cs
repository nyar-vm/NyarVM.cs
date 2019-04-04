using System;

namespace Core.Widget.Style;

/// <summary>
///     ApplyStyle 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ApplyStyleAttribute : Attribute
{
    /// <summary>
    ///     要应用的样式键名
    /// </summary>
    public string? key { get; set; }
}