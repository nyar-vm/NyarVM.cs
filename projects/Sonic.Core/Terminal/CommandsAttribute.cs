using System;

namespace Core.Terminal;

/// <summary>
///     标记枚举类型或枚举类型属性为子命令集合。
///     标记在枚举上时，表示该枚举定义了一组子命令。
///     标记在属性上时，表示该属性是子命令分发器，属性类型必须为标记了 <c>[Commands]</c> 的枚举。
///     子命令名称从枚举成员名自动推断（转为 kebab-case）。
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property)]
public sealed class CommandsAttribute : Attribute
{
    /// <summary>
    ///     获取或设置默认子命令的枚举成员名称。
    /// </summary>
    public string? default_command { get; init; }
}