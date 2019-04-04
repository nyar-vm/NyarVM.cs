using Std.Command.Metadata;

namespace Std.Command.Ports;

/// <summary>
///     命令路由器端口，负责将输入文本解析为对应命令
///     REPL 和 TUI 内部控制台都需要此能力，CLI 的一次性模式不需要
/// </summary>
public interface ICommandRouter
{
    /// <summary>
    ///     根据输入文本解析匹配的命令类型
    /// </summary>
    /// <param name="input">输入的命令文本</param>
    /// <returns>匹配的命令类型，未匹配时返回 null</returns>
    Type? resolve_command(string input);

    /// <summary>
    ///     获取指定前缀的补全候选项
    /// </summary>
    /// <param name="prefix">输入前缀</param>
    /// <returns>补全候选命令名列表</returns>
    IReadOnlyList<string> get_completions(string prefix);

    /// <summary>
    ///     获取所有已注册的命令元数据
    /// </summary>
    /// <returns>命令信息列表</returns>
    IReadOnlyList<CommandInfo> get_commands();
}