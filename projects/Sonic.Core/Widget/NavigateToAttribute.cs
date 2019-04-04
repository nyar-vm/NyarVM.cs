using System;

namespace Core.Widget;

/// <summary>
///     NavigateTo 属性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NavigateToAttribute : Attribute
{
    /// <summary>
    ///     目标路由
    /// </summary>
    public string? route { get; set; }
}