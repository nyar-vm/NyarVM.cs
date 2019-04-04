using System;

namespace Core.Command.Option;

/// <summary>
///     选项特性，标注命名选项（如 --verbose、-v）
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute : Attribute
{
    /// <summary>
    ///     标注同时具有长短名称的选项
    /// </summary>
    /// <param name="shortName">短名称</param>
    /// <param name="longName">长名称</param>
    /// <param name="description">选项描述</param>
    public OptionAttribute(char shortName, string longName, string description)
    {
        short_name = shortName;
        long_name = longName;
        this.description = description;
    }

    /// <summary>
    ///     标注仅具有长名称的选项
    /// </summary>
    /// <param name="longName">长名称</param>
    /// <param name="description">选项描述</param>
    public OptionAttribute(string longName, string description)
    {
        long_name = longName;
        this.description = description;
    }

    /// <summary>
    ///     短名称（如 'v' 对应 -v）
    /// </summary>
    public char? short_name { get; }

    /// <summary>
    ///     长名称（如 "verbose" 对应 --verbose）
    /// </summary>
    public string? long_name { get; }

    /// <summary>
    ///     选项描述
    /// </summary>
    public string description { get; }

    /// <summary>
    ///     默认值
    /// </summary>
    public object? @default { get; set; }

    /// <summary>
    ///     选项别名
    /// </summary>
    public string[] alias { get; set; } = [];

    /// <summary>
    ///     环境变量名，参数值可从此环境变量读取
    /// </summary>
    public string? environment_variable { get; set; }

    /// <summary>
    ///     配置键，参数值可从此配置键读取
    /// </summary>
    public string? config_key { get; set; }
}