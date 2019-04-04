using System;

namespace Core.Command;

/// <summary>
///     命令处理程序特性，将枚举值绑定到具体的处理程序类
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CommandHandlerAttribute : Attribute
{
    /// <summary>
    ///     绑定子命令处理程序
    /// </summary>
    /// <param name="subCommandType">子命令枚举值（如 typeof(DbSubCommand.Migrate)）</param>
    public CommandHandlerAttribute(Type subCommandType)
    {
        sub_command_type = subCommandType;
    }

    /// <summary>
    ///     子命令枚举值类型
    /// </summary>
    public Type sub_command_type { get; }
}