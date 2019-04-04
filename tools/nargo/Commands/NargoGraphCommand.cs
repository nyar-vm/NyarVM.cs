using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nargo.Cli.Commands;
using Nargo.Cli.ProjectModel;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo graph 命令：显示工程图、依赖图、入口图
/// </summary>
[Command("graph", "显示工程图、依赖图、入口图")]
public sealed class NargoGraphCommand : ICommand
{
    /// <summary>
    ///     项目目录（默认当前目录）
    /// </summary>
    [Option('d', "dir", "项目目录")]
    public string? directory { get; set; }

    /// <summary>
    ///     是否显示依赖图
    /// </summary>
    [Option("deps", "显示依赖图")]
    public bool show_deps { get; set; }

    /// <summary>
    ///     执行 graph 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var directoryPath = directory ?? Directory.GetCurrentDirectory();

        var (result, _) = ImportCommand.execute(directoryPath);

        if (result is NargoWorkspace workspace)
        {
            GraphCommand.show_workspace(workspace);
        }
        else if (result is NargoProject project)
        {
            GraphCommand.show_project(project);
        }
        else
        {
            Console.Error.WriteLine("未找到可识别的工程");
            return Task.FromResult(ExitCode.Error);
        }

        return Task.FromResult(ExitCode.Success);
    }
}
