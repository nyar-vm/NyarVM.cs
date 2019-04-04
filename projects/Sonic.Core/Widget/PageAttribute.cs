using System;

namespace Core.Widget;

/// <summary>
///     Page 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PageAttribute : Attribute
{
    /// <summary>
    ///     页面路由
    /// </summary>
    public string? route { get; set; }
}