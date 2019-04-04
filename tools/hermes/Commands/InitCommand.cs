using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Hermes.CLI.Commands;

/// <summary>
///     init 子命令：初始化 Hermes 项目结构
/// </summary>
[Command("init", "初始化 Hermes 项目结构")]
internal sealed class InitCommand : ICommand
{
    /// <summary>
    ///     项目名称（可选，默认当前目录）
    /// </summary>
    [Argument(0, "项目名称（可选，默认当前目录）")]
    public string? name { get; set; }

    /// <summary>
    ///     强制覆盖现有文件
    /// </summary>
    [Option('f', "force", "强制覆盖现有文件")]
    public bool force { get; set; }

    /// <summary>
    ///     创建示例 Schema
    /// </summary>
    [Option('s', "with-schema", "创建示例 Schema")]
    public bool with_schema { get; set; } = true;

    /// <summary>
    ///     执行 init 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        HermesCommands.init(name, force, with_schema);
        return Task.FromResult(ExitCode.Success);
    }
}
