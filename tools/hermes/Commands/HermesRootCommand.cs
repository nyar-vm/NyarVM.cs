using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Hermes.CLI.Commands;

/// <summary>
///     Hermes 根命令，聚合所有子命令
/// </summary>
[Command("hermes", "Hermes Schema 驱动代码生成框架")]
internal sealed class HermesRootCommand : ICommand
{
    /// <summary>
    ///     generate 子命令：根据 Schema 生成代码
    /// </summary>
    [Subcommand]
    public GenerateCommand? generate { get; set; }

    /// <summary>
    ///     validate 子命令：验证 Schema 文件语法
    /// </summary>
    [Subcommand]
    public ValidateCommand? validate { get; set; }

    /// <summary>
    ///     diff 子命令：比较两个 Schema 文件或目录的差异
    /// </summary>
    [Subcommand]
    public DiffCommand? diff { get; set; }

    /// <summary>
    ///     init 子命令：初始化 Hermes 项目结构
    /// </summary>
    [Subcommand]
    public InitCommand? init { get; set; }

    /// <summary>
    ///     执行根命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        return Task.FromResult(ExitCode.Success);
    }
}
