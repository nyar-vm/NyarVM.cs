using System;

namespace Core.Terminal;

/// <summary>
///     标记属性为子命令。构造函数接收子命令类型，Source Generator 将为标记的属性生成子命令分发逻辑。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SubcommandAttribute : Attribute
{
    /// <summary>
    ///     初始化子命令特性，指定子命令类型。
    /// </summary>
    /// <param name="subcommandType">子命令的类型，必须为标记了 <c>[Command]</c> 的类。</param>
    public SubcommandAttribute(Type subcommandType)
    {
        subcommand_type = subcommandType;
    }

    /// <summary>
    ///     获取子命令类型。
    /// </summary>
    public Type subcommand_type { get; }
}