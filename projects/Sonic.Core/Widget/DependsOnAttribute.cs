using System;

namespace Core.Widget;

/// <summary>
///     DependsOn 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>
    ///     依赖的源属性名称
    /// </summary>
    public string? source { get; set; }
}