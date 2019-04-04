using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa new 命令：创建新项目
/// </summary>
[Command("new", "创建新项目")]
public sealed class VoaNewCommand : ICommand
{
    /// <summary>
    ///     项目名称
    /// </summary>
    [Argument(0, "项目名称")]
    public string name { get; set; } = "";

    /// <summary>
    ///     项目模板
    /// </summary>
    [Option('t', "template", "项目模板")]
    public string template { get; set; } = "webapp";

    /// <summary>
    ///     编译目标
    /// </summary>
    [Option("target", "编译目标")]
    public string target { get; set; } = "wasm";

    /// <summary>
    ///     执行 new 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var exitCode = NewCommand.execute(name, template, target);
        return Task.FromResult(exitCode == 0 ? ExitCode.Success : ExitCode.Error);
    }
}
