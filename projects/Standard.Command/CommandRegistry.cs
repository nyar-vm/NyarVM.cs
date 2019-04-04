using Core.Command;
using Std.Command.Metadata;

namespace Std.Command;

/// <summary>
///     全局命令注册表，管理所有已注册的命令
/// </summary>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, CommandInfo> _command_infos = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _commands = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     注册命令类型，使用类型名称作为命令名称
    /// </summary>
    /// <typeparam name="T">实现了 ICommand 的类型</typeparam>
    public void register<T>() where T : ICommand
    {
        register(typeof(T).Name, typeof(T));
    }

    /// <summary>
    ///     使用指定名称注册命令类型
    /// </summary>
    /// <typeparam name="T">实现了 ICommand 的类型</typeparam>
    /// <param name="name">命令名称</param>
    public void register<T>(string name) where T : ICommand
    {
        register(name, typeof(T));
    }

    /// <summary>
    ///     注册命令类型
    /// </summary>
    /// <param name="commandType">命令类型</param>
    public void register(Type commandType)
    {
        register(commandType.Name, commandType);
    }

    /// <summary>
    ///     使用指定名称注册命令类型
    /// </summary>
    /// <param name="name">命令名称</param>
    /// <param name="commandType">命令类型</param>
    public void register(string name, Type commandType)
    {
        _commands[name] = commandType;
        _command_infos[name] = new CommandInfo
        {
            name = name,
            command_type = commandType
        };
    }

    /// <summary>
    ///     根据命令名称查找命令类型
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>命令类型，未找到时返回 null</returns>
    public Type? resolve(string commandName)
    {
        _commands.TryGetValue(commandName, out var type);
        return type;
    }

    /// <summary>
    ///     获取所有已注册的命令信息
    /// </summary>
    /// <returns>命令信息集合</returns>
    public IEnumerable<CommandInfo> get_commands()
    {
        return _command_infos.Values;
    }

    /// <summary>
    ///     移除指定命令
    /// </summary>
    /// <param name="commandName">命令名称</param>
    /// <returns>是否成功移除</returns>
    public bool unregister(string commandName)
    {
        _command_infos.Remove(commandName);
        return _commands.Remove(commandName);
    }

    /// <summary>
    ///     清空所有已注册的命令
    /// </summary>
    public void clear()
    {
        _commands.Clear();
        _command_infos.Clear();
    }
}