using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.Language.Valkyrie.TypeChecker;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;
using Std.Data.Text.Valkyrie.Semantic;
using CompilationUnit = Std.Data.Text.Valkyrie.AST.ProgramRoot;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     check 子命令：类型检查源文件
/// </summary>
[Command("check", "类型检查源文件")]
public sealed class VccCheckCommand : ICommand
{
    /// <summary>
    ///     目标路径（文件或目录）
    /// </summary>
    [Argument(0, "目标路径（文件或目录）")]
    public string? path { get; set; }

    /// <summary>
    ///     详细输出
    /// </summary>
    [Option('v', "verbose", "详细输出")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 check 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var diagnostics = new DiagnosticSink();
        var lexer = new ValkyrieLexer(diagnostics);
        var parser = new ValkyrieParser();
        var typeChecker = new TypeChecker();

        var totalFiles = 0;
        var totalErrors = 0;
        var totalWarnings = 0;

        var targetPath = path ?? ".";
        var vFiles = Directory.Exists(targetPath)
            ? Directory.GetFiles(targetPath, "*.v", SearchOption.AllDirectories)
            : File.Exists(targetPath) && targetPath.EndsWith(".v")
                ? [targetPath]
                : [];

        foreach (var vFile in vFiles)
        {
            totalFiles++;
            var fileName = Path.GetFileName(vFile);

            try
            {
                var source = File.ReadAllText(vFile);
                var tokens = lexer.tokenize(source);
                var ast = parser.parse(tokens) as CompilationUnit;
                if (ast == null) continue;

                if (diagnostics.has_errors)
                {
                    var diags = diagnostics.get_diagnostics();
                    totalErrors += diagnostics.get_diagnostics().Length;
                    foreach (var error in diags)
                    {
                        Console.WriteLine($"  {fileName}: 语法错误 - {error.message}");
                    }

                    diagnostics.clear();
                    continue;
                }

                var typeResult = typeChecker.check(ast, vFile);
                if (typeResult.has_errors || typeResult.has_warnings)
                {
                    foreach (var diag in typeResult.diagnostics)
                    {
                        var location = diag.line > 0 ? $"{fileName}({diag.line}:{diag.column}): " : $"{fileName}: ";
                        Console.WriteLine(
                            $"  {location}{diag.severity.ToString().ToLower()} {diag.code}: {diag.message}");
                    }

                    totalErrors += typeResult.diagnostics.Count(d => d.severity == DiagnosticSeverity.error);
                    totalWarnings += typeResult.diagnostics.Count(d => d.severity == DiagnosticSeverity.warning);
                }
                else if (verbose)
                {
                    Console.WriteLine($"  {fileName}: 类型检查通过");
                }

                diagnostics.clear();
            }
            catch (IOException ex)
            {
                totalErrors++;
                Console.WriteLine($"  {fileName}: IO 错误 - {ex.Message}");
            }
        }

        Console.WriteLine($"  共 {totalFiles} 个文件，{totalErrors} 个错误，{totalWarnings} 个警告");
        return Task.FromResult(totalErrors > 0 ? ExitCode.Error : ExitCode.Success);
    }
}
