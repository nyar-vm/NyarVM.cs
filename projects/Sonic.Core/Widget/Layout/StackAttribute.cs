using System;

namespace Core.Widget.Layout;

/// <summary>
///     Stack 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StackAttribute : Attribute
{
    /// <summary>
    ///     排列方向
    /// </summary>
    public Orientation orientation { get; set; } = Orientation.vertical;
}