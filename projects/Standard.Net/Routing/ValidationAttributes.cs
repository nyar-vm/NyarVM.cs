namespace Std.Net.Routing;

/// <summary>
///     必填验证属性，标记字段不能为空或空白字符串。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiredAttribute : Attribute
{
}

/// <summary>
///     最大长度验证属性，限制字符串字段的最大字符数。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MaxLengthAttribute : Attribute
{
    /// <summary>
    ///     初始化最大长度验证。
    /// </summary>
    /// <param name="length">最大长度</param>
    public MaxLengthAttribute(int length)
    {
        this.length = length;
    }

    /// <summary>
    ///     最大允许长度。
    /// </summary>
    public int length { get; }
}

/// <summary>
///     最小长度验证属性，限制字符串字段的最小字符数。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MinLengthAttribute : Attribute
{
    /// <summary>
    ///     初始化最小长度验证。
    /// </summary>
    /// <param name="length">最小长度</param>
    public MinLengthAttribute(int length)
    {
        this.length = length;
    }

    /// <summary>
    ///     最小必须长度。
    /// </summary>
    public int length { get; }
}

/// <summary>
///     数值范围验证属性，限制数值字段的最小值和最大值。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RangeAttribute : Attribute
{
    /// <summary>
    ///     初始化范围验证。
    /// </summary>
    /// <param name="minimum">最小值</param>
    /// <param name="maximum">最大值</param>
    public RangeAttribute(int minimum, int maximum)
    {
        this.minimum = minimum;
        this.maximum = maximum;
    }

    /// <summary>
    ///     最小值。
    /// </summary>
    public IComparable minimum { get; }

    /// <summary>
    ///     最大值。
    /// </summary>
    public IComparable maximum { get; }
}

/// <summary>
///     字符串长度验证属性，同时限制最小和最大字符数。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StringLengthAttribute : Attribute
{
    /// <summary>
    ///     初始化字符串长度验证。
    /// </summary>
    /// <param name="maximumLength">最大长度</param>
    public StringLengthAttribute(int maximumLength)
    {
        maximum_length = maximumLength;
        minimum_length = 0;
    }

    /// <summary>
    ///     最大允许长度。
    /// </summary>
    public int maximum_length { get; }

    /// <summary>
    ///     最小允许长度，默认为 0。
    /// </summary>
    public int minimum_length { get; set; }
}

/// <summary>
///     邮箱验证属性，验证字符串是否为有效的邮箱地址格式。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EmailAttribute : Attribute
{
}