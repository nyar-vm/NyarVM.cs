using System;

namespace Core.Terminal;

/// <summary>
///     标记属性为位置参数。位置参数按顺序从命令行参数中读取，无需显式命名。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ArgumentAttribute : Attribute
{
    /// <summary>
    ///     初始化 CLI 位置参数特性。
    /// </summary>
    /// <param name="position">位置索引，-1 表示自动推断。</param>
    public ArgumentAttribute(int position = -1)
    {
        this.position = position;
    }

    /// <summary>
    ///     获取位置索引，-1 表示自动推断。
    /// </summary>
    public int position { get; init; }

    /// <summary>
    ///     获取或设置参数描述。若未设置，Source Generator 将读取属性的 <c>&lt;summary&gt;</c> XML 注释。
    /// </summary>
    public string? description { get; init; }

    /// <summary>
    ///     获取或设置默认值。
    /// </summary>
    public object? default_value { get; init; }

    /// <summary>
    ///     获取或设置是否必需。
    /// </summary>
    public bool required { get; init; } = true;

    /// <summary>
    ///     获取或设置值名称提示。
    /// </summary>
    public string? value_name { get; init; }

    /// <summary>
    ///     获取或设置本地化资源键。
    /// </summary>
    public string? resource_key { get; init; }
}