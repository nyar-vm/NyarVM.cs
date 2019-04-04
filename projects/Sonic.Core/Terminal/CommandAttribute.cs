using System;

namespace Core.Terminal;

/// <summary>
///     标记 partial 类为 CLI 命令。命令名称从类名自动推断（去掉 Command 后缀后转为 kebab-case）。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    ///     获取或设置命令描述。若未设置，Source Generator 将读取类的 <c>&lt;summary&gt;</c> XML 注释。
    /// </summary>
    public string? description { get; init; }

    /// <summary>
    ///     获取或设置命令详细描述。
    /// </summary>
    public string? long_description { get; init; }

    /// <summary>
    ///     获取或设置命令别名。
    /// </summary>
    public string[]? aliases { get; init; }

    /// <summary>
    ///     获取或设置是否在帮助中隐藏。
    /// </summary>
    public bool hide { get; init; }

    /// <summary>
    ///     获取或设置命令版本。
    /// </summary>
    public string? version { get; init; }

    /// <summary>
    ///     获取或设置环境变量前缀。
    /// </summary>
    public string? env_prefix { get; init; }

    /// <summary>
    ///     获取或设置本地化资源键。
    /// </summary>
    public string? resource_key { get; init; }
}