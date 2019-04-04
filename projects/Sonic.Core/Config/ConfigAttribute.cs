using System;

namespace Core.Config;

/// <summary>
///     标记配置对象，控制配置绑定和验证的自动生成。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ConfigAttribute : Attribute
{
    /// <summary>
    ///     配置节名称。
    /// </summary>
    public string? section { get; set; }

    /// <summary>
    ///     是否生成配置绑定器。
    /// </summary>
    public bool generate_binder { get; set; } = true;

    /// <summary>
    ///     是否生成验证器。
    /// </summary>
    public bool generate_validator { get; set; } = true;

    /// <summary>
    ///     属性名到配置键名的命名约定，对应 <see cref="NamingConvention" /> 枚举值。
    /// </summary>
    public int naming { get; set; } = 0;
}