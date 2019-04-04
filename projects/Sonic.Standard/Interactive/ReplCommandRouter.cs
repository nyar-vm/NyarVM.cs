using Sonic.Command;
using Sonic.Command.Metadata;
using Sonic.Command.Ports;

namespace Sonic.Interactive;

/// <summary>
/// REPL 命令路由器，实现 ICommandRouter
/// 根据输入文本的首个 Token 匹配命令类型，提供补全候选项
/// </summary>
public sealed class ReplCommandRouter : ICommandRouter
{
    private readonly CommandRegistry _registry;

    /// <summary>
    /// 创建 REPL 命令路由器
    /// </summary>
    /// <param name="registry">命令注册表</param>
    public ReplCommandRouter(CommandRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public Type? resolve_command(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return null;
        }

        var commandName = parts[0];
        return _registry.resolve(commandName);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> get_completions(string prefix)
    {
        var completions = new List<string>();

        foreach (var info in _registry.get_commands())
        {
            if (info.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                completions.Add(info.name);
            }
        }

        return completions;
    }

    /// <inheritdoc />
    public IReadOnlyList<CommandInfo> get_commands()
    {
        return _registry.get_commands().ToList();
    }
}
