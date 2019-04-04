using System;

namespace Core.Data;

/// <summary>
///     标记字段使用自定义验证逻辑，指定验证器类型。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CustomValidationAttribute : Attribute
{
    /// <summary>
    ///     初始化自定义验证特性。
    /// </summary>
    /// <param name="validatorType">自定义验证器的类型。</param>
    public CustomValidationAttribute(Type validatorType)
    {
        validator_type = validatorType;
    }

    /// <summary>
    ///     自定义验证器的类型。
    /// </summary>
    public Type validator_type { get; }

    /// <summary>
    ///     验证失败时的自定义错误消息。
    /// </summary>
    public string? error_message { get; set; }
}