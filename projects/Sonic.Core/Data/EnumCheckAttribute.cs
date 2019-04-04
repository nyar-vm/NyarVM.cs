using System;

namespace Core.Data;

/// <summary>
///     验证枚举字段的值是否在枚举定义范围内。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EnumCheckAttribute : Attribute
{
    /// <summary>
    ///     验证失败时的自定义错误消息。
    /// </summary>
    public string? error_message { get; set; }
}