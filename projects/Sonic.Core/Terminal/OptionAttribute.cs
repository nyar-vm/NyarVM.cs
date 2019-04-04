using System;

namespace Core.Terminal;

/// <summary>
///     标记属性为命名选项。选项长名称从属性名自动推断（转为 kebab-case），
///     短名称通过构造函数参数指定。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute : Attribute
{
    /// <summary>
    ///     初始化命名选项特性，名称从属性名自动推断。
    /// </summary>
    public OptionAttribute()
    {
        long_name = null;
        short_name = '\0';
    }

    /// <summary>
    ///     初始化命名选项特性，指定短名称，长名称从属性名自动推断。
    /// </summary>
    /// <param name="shortName">选项短名称。</param>
    public OptionAttribute(char shortName)
    {
        short_name = shortName;
        long_name = null;
    }

    /// <summary>
    ///     初始化命名选项特性，指定长名称。
    /// </summary>
    /// <param name="longName">选项长名称。</param>
    public OptionAttribute(string longName)
    {
        long_name = longName;
        short_name = '\0';
    }

    /// <summary>
    ///     初始化命名选项特性，指定短名称和长名称。
    /// </summary>
    /// <param name="shortName">选项短名称。</param>
    /// <param name="longName">选项长名称。</param>
    public OptionAttribute(char shortName, string longName)
    {
        short_name = shortName;
        long_name = longName;
    }

    /// <summary>
    ///     获取选项长名称。为 null 时从属性名自动推断（转为 kebab-case）。
    /// </summary>
    public string? long_name { get; }

    /// <summary>
    ///     获取选项短名称，'\0' 表示未设置。
    /// </summary>
    public char short_name { get; }

    /// <summary>
    ///     获取或设置选项描述。若未设置，Source Generator 将读取属性的 <c>&lt;summary&gt;</c> XML 注释。
    /// </summary>
    public string? description { get; init; }

    /// <summary>
    ///     获取或设置默认值。
    /// </summary>
    public object? default_value { get; init; }

    /// <summary>
    ///     获取或设置是否必需。
    /// </summary>
    public bool required { get; init; }

    /// <summary>
    ///     获取或设置值名称提示。
    /// </summary>
    public string? value_name { get; init; }

    /// <summary>
    ///     获取或设置是否接受值，布尔类型默认为 false。
    /// </summary>
    public bool takes_value { get; init; } = true;

    /// <summary>
    ///     获取或设置环境变量名。
    /// </summary>
    public string? env { get; init; }

    /// <summary>
    ///     获取或设置是否在帮助中隐藏。
    /// </summary>
    public bool hide { get; init; }

    /// <summary>
    ///     获取或设置本地化资源键。
    /// </summary>
    public string? resource_key { get; init; }
}