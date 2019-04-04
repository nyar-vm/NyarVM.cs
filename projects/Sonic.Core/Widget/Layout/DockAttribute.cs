using System;

namespace Core.Widget.Layout;

/// <summary>
///     Dock 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DockAttribute : Attribute
{
    /// <summary>
    ///     停靠位置
    /// </summary>
    public DockPosition position { get; set; } = DockPosition.left;
}