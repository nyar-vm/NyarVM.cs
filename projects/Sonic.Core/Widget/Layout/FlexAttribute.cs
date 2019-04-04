using System;

namespace Core.Widget.Layout;

/// <summary>
///     Flex 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class FlexAttribute : Attribute
{
    /// <summary>
    ///     主轴方向
    /// </summary>
    public FlexDirection direction { get; set; } = FlexDirection.row;
}