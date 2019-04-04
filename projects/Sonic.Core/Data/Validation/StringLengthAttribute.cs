using System;

namespace Core.Data.Validation;

/// <summary>
///     验证字符串字段长度的最小值和最大值。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StringLengthAttribute : Attribute
{
    /// <summary>
    ///     初始化字符串长度验证特性。
    /// </summary>
    /// <param name="minLength">最小长度。</param>
    /// <param name="maxLength">最大长度。</param>
    public StringLengthAttribute(int minLength, int maxLength)
    {
        min_length = minLength;
        max_length = maxLength;
    }

    /// <summary>
    ///     最小长度。
    /// </summary>
    public int min_length { get; }

    /// <summary>
    ///     最大长度。
    /// </summary>
    public int max_length { get; }

    /// <summary>
    ///     验证失败时的自定义错误消息。
    /// </summary>
    public string? error_message { get; set; }
}