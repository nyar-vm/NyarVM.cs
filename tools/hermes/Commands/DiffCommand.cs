using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Hermes.CLI.Commands;

/// <summary>
///     diff 子命令：比较两个 Schema 文件或目录的差异
/// </summary>
[Command("diff", "比较两个 Schema 文件或目录的差异")]
internal sealed class DiffCommand : ICommand
{
    /// <summary>
    ///     旧 Schema 文件或目录路径
    /// </summary>
    [Argument(0, "旧 Schema 文件或目录路径")]
    public string? old { get; set; }

    /// <summary>
    ///     新 Schema 文件或目录路径
    /// </summary>
    [Argument(1, "新 Schema 文件或目录路径")]
    public string? @new { get; set; }

    /// <summary>
    ///     输出 JSON 格式差异
    /// </summary>
    [Option('j', "json", "输出 JSON 格式差异")]
    public bool json { get; set; }

    /// <summary>
    ///     仅显示变更
    /// </summary>
    [Option('c', "changes-only", "仅显示变更")]
    public bool changes_only { get; set; }

    /// <summary>
    ///     执行 diff 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        HermesCommands.diff(old, @new, json, changes_only);
        return Task.FromResult(ExitCode.Success);
    }
}
