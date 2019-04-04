using System;

namespace Core.Finance;

/// <summary>
///     标记金融数值的精度属性或字段
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PrecisionAttribute : Attribute
{
    /// <summary>
    ///     小数位数
    /// </summary>
    public int decimal_digits { get; set; } = 2;
}