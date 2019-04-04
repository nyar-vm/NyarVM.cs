using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;
using Std.Data.Text.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Legion.CLI.Commands;

/// <summary>
///     legion lint 命令：代码静态分析
/// </summary>
[Command("lint", "代码静态分析")]
public sealed class LegionLintCommand : ICommand
{
    private static readonly object ConsoleLock = new();

    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     诊断输出格式：<c>pretty</c> / <c>short</c> / <c>detail</c> / <c>json</c>
    /// </summary>
    [Option("format", "诊断输出格式：pretty / short / detail / json")]
    public string? format { get; set; }

    /// <summary>
    ///     最小诊断级别：<c>fatal</c> / <c>error</c> / <c>warning</c> / <c>info</c> / <c>hint</c> / <c>debug</c> / <c>trace</c>
    /// </summary>
    [Option("level", "最小诊断级别：fatal / error / warning / info / hint / debug / trace")]
    public string? level { get; set; }

    /// <summary>
    ///     诊断颜色模式：<c>auto</c> / <c>always</c> / <c>never</c>
    /// </summary>
    [Option("color", "诊断颜色模式：auto / always / never")]
    public string? color { get; set; }

    /// <summary>
    ///     执行 lint 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        if (!LegionHelper.try_resolve_diagnostic_options(
                projectDir,
                format,
                level,
                color,
                null,
                out var diagnosticOptions,
                out var diagnosticError))
        {
            Console.Error.WriteLine($"错误：{diagnosticError}");
            return Task.FromResult(ExitCode.Error);
        }

        var vFiles = LegionHelper.get_project_v_files(projectDir);
        if (vFiles.Length == 0)
        {
            Console.WriteLine("未找到 .v 文件");
            return Task.FromResult(ExitCode.Success);
        }

        var totalErrors = 0;
        var totalWarnings = 0;

        Parallel.ForEach(vFiles, file =>
        {
            try
            {
                var source = File.ReadAllText(file);
                var compiler = LegionHelper.get_cached_valkyrie_compiler(projectDir);
                var tokens = compiler.lex_file(file);
                if (compiler.diagnostics.has_errors)
                {
                    lock (ConsoleLock)
                    {
                        var counts = DiagnosticRenderer.write_lint_diagnostics(
                            file,
                            source,
                            compiler.diagnostics.messages,
                            diagnosticOptions,
                            projectDir);
                        Interlocked.Add(ref totalErrors, counts.errors);
                        Interlocked.Add(ref totalWarnings, counts.warnings);
                    }

                    return;
                }

                _ = compiler.parse(tokens);
                if (compiler.diagnostics.messages.Count > 0)
                {
                    lock (ConsoleLock)
                    {
                        var counts = DiagnosticRenderer.write_lint_diagnostics(
                            file,
                            source,
                            compiler.diagnostics.messages,
                            diagnosticOptions,
                            projectDir);
                        Interlocked.Add(ref totalErrors, counts.errors);
                        Interlocked.Add(ref totalWarnings, counts.warnings);
                    }
                }
            }
            catch (Exception ex)
            {
                lock (ConsoleLock)
                {
                    Console.Error.WriteLine($"lint 失败 {file}：{ex.Message}");
                    Interlocked.Increment(ref totalErrors);
                }
            }
        });

        DiagnosticRenderer.write_summary(diagnosticOptions, "lint", totalErrors, totalWarnings);
        return Task.FromResult(totalErrors > 0 ? ExitCode.Error : ExitCode.Success);
    }
}
