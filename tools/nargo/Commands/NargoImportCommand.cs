using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nargo.Cli.Compatibility;
using ICommand = Core.Command.ICommand;

namespace Nargo.Cli.Commands;

/// <summary>
///     nargo import 命令：导入现有 npm / pnpm 项目
/// </summary>
[Command("import", "导入现有 npm / pnpm 项目")]
public sealed class NargoImportCommand : ICommand
{
    /// <summary>
    ///     项目目录（默认当前目录）
    /// </summary>
    [Option('d', "dir", "项目目录")]
    public string? directory { get; set; }

    /// <summary>
    ///     执行 import 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var directoryPath = directory ?? Directory.GetCurrentDirectory();
        var (result, report) = ImportCommand.execute(directoryPath);

        if (report.HasErrors)
        {
            Console.Error.WriteLine(report.to_report_text());
            return Task.FromResult(ExitCode.Error);
        }

        if (report.HasWarnings)
        {
            Console.WriteLine(report.to_report_text());
        }

        return Task.FromResult(ExitCode.Success);
    }
}
