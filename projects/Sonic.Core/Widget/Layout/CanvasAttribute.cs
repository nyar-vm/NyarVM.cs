using System;

namespace Core.Widget.Layout;

/// <summary>
///     Canvas 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CanvasAttribute : Attribute
{
    /// <summary>
    ///     画布宽度
    /// </summary>
    public int width { get; set; }


    /// <summary>
    ///     画布高度
    /// </summary>
    public int height { get; set; }
}