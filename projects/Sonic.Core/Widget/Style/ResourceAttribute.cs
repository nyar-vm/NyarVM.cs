using System;

namespace Core.Widget.Style;

/// <summary>
///     Resource 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ResourceAttribute : Attribute
{
    /// <summary>
    ///     资源键名
    /// </summary>
    public string? key { get; set; }
}