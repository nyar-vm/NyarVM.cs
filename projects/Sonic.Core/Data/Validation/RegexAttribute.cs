using System;

namespace Core.Data.Validation;

/// <summary>
///     验证字符串字段是否匹配指定的正则表达式模式。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RegexAttribute : Attribute
{
    /// <summary>
    ///     初始化正则表达式验证特性。
    /// </summary>
    /// <param name="pattern">正则表达式模式。</param>
    public RegexAttribute(string pattern)
    {
        this.pattern = pattern;
    }

    /// <summary>
    ///     正则表达式模式。
    /// </summary>
    public string pattern { get; }

    /// <summary>
    ///     验证失败时的自定义错误消息。
    /// </summary>
    public string? error_message { get; set; }
}