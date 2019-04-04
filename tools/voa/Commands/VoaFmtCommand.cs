using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;
using Nyar.Language.Valkyrie.Formatter;
using ICommand = Core.Command.ICommand;

namespace Asgard.CLI.Commands;

/// <summary>
///     voa fmt 命令：格式化代码
/// </summary>
[Command("fmt", "格式化代码")]
public sealed class VoaFmtCommand : ICommand
{
    /// <summary>
    ///     格式化路径（默认当前目录）
    /// </summary>
    [Argument(0, "格式化路径")]
    public string? path { get; set; }

    /// <summary>
    ///     执行 fmt 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var targetPath = path ?? ".";
        if (!Directory.Exists(targetPath))
        {
            Console.Error.WriteLine($"错误：路径不存在 '{targetPath}'");
            return Task.FromResult(ExitCode.Error);
        }

        var vFiles = Directory.GetFiles(targetPath, "*.v", SearchOption.AllDirectories);
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
                var diagnostics = new DiagnosticSink();
                var lexer = new ValkyrieLexer(diagnostics);
                var tokens = lexer.tokenize(source);
                if (diagnostics.has_errors)
                {
                    Console.Error.WriteLine($"词法错误：{file}");
                    continue;
                }

                var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
                var node = parser.parse(tokens);
                if (diagnostics.has_errors)
                {
                    Console.Error.WriteLine($"语法错误：{file}");
                    continue;
                }

                if (node is not CompilationUnit unit)
                {
                    Console.Error.WriteLine($"解析错误：{file}（不是有效的编译单元）");
                    continue;
                }

                var formatted = ValkyrieFormatter.Format(unit);
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
