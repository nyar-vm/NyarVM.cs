using System;

namespace Core.Widget.Island;

/// <summary>
///     Island 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IslandAttribute : Attribute
{
    /// <summary>
    ///     渲染模式
    /// </summary>
    public RenderMode render_mode { get; set; } = RenderMode.server;
}