using System;

namespace Core.Widget.Style;

/// <summary>
///     Variant 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class VariantAttribute : Attribute
{
    /// <summary>
    ///     变体名称
    /// </summary>
    public string? name { get; set; }
}