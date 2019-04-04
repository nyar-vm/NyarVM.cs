using System;

namespace Core.Widget.Layout;

/// <summary>
///     UniformGrid 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UniformGridAttribute : Attribute
{
    /// <summary>
    ///     行数
    /// </summary>
    public int rows { get; set; } = 1;


    /// <summary>
    ///     列数
    /// </summary>
    public int columns { get; set; } = 1;
}