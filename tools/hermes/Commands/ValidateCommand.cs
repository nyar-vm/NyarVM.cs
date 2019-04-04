using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Hermes.CLI.Commands;

/// <summary>
///     validate 子命令：验证 Schema 文件语法，输出诊断信息
/// </summary>
[Command("validate", "验证 Schema 文件语法，输出诊断信息")]
internal sealed class ValidateCommand : ICommand
{
    /// <summary>
    ///     Schema 文件或目录路径
    /// </summary>
    [Argument(0, "Schema 文件或目录路径")]
    public string? path { get; set; }

    /// <summary>
    ///     安静模式，只输出错误
    /// </summary>
    [Option('q', "quiet", "安静模式，只输出错误")]
    public bool quiet { get; set; }

    /// <summary>
    ///     输出 JSON 格式诊断
    /// </summary>
    [Option('j', "json", "输出 JSON 格式诊断")]
    public bool json { get; set; }

    /// <summary>
    ///     执行 validate 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        HermesCommands.validate(path, quiet, json);
        return Task.FromResult(ExitCode.Success);
    }
}
