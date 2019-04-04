using System;

namespace Core.Widget;

/// <summary>
///     Route 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RouteAttribute : Attribute
{
    /// <summary>
    ///     路由模式
    /// </summary>
    public string? pattern { get; set; }
}