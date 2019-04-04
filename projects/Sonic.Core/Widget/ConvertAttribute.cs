using System;

namespace Core.Widget;

/// <summary>
///     Convert 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConvertAttribute : Attribute
{
    /// <summary>
    ///     值转换器类型
    /// </summary>
    public Type? converter_type { get; set; }
}