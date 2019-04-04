using Core.Command;
using Core.Command.Argument;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;
using Nyar.Language.Valkyrie.Formatter;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;

namespace Legion.CLI.Commands;

/// <summary>
///     legion fmt 命令：格式化代码
/// </summary>
[Command("fmt", "格式化代码")]
public sealed class LegionFmtCommand : ICommand
{
    /// <summary>
    ///     项目路径
    /// </summary>
    [Argument(0, "项目路径")]
    public string? project { get; set; }

    /// <summary>
    ///     执行 fmt 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var projectDir = LegionHelper.resolve_project_dir(project);
        if (projectDir is null)
        {
            Console.Error.WriteLine($"错误：找不到项目 '{project}'");
            return Task.FromResult(ExitCode.Error);
        }

        var vFiles = LegionHelper.get_project_v_files(projectDir);
        if (vFiles.Length == 0)
        {
            Console.WriteLine("未找到 .v 文件");
            return Task.FromResult(ExitCode.Success);
        }

        var totalFormatted = 0;
        foreach (var file in vFiles)
        {
            try
            {
                var source = File.ReadAllText(file);
                var diagnosticsCollector = new DiagnosticSink();
                var lexer = new ValkyrieLexer(diagnosticsCollector);
                var tokens = lexer.tokenize(source);
                if (diagnosticsCollector.has_errors)
                {
                    Console.Error.WriteLine($"跳过（词法错误）：{file}");
                    continue;
                }

                var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnosticsCollector);
                var unit = parser.parse(tokens);
                if (diagnosticsCollector.has_errors)
                {
                    Console.Error.WriteLine($"跳过（语法错误）：{file}");
                    continue;
                }

                var formatted = ValkyrieFormatter.Format((ProgramRoot)unit);
                if (formatted != source)
                {
                    File.WriteAllText(file, formatted);
                    totalFormatted++;
                    Console.WriteLine($"已格式化：{file}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"格式化失败 {file}：{ex.Message}");
            }
        }

        Console.WriteLine($"格式化完成，共 {totalFormatted} 个文件");
        return Task.FromResult(ExitCode.Success);
    }
}
