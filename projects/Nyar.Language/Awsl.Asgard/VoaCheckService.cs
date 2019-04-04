using Nyar.Language.Valkyrie.TypeChecker;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;

namespace Valkyrie.Asgard;

/// <summary>
///     VOA 代码检查服务 — 对 V 文件执行词法分析、语法分析和类型检查
/// </summary>
public static class VoaCheckService
{
    /// <summary>
    ///     对指定路径的 V 文件执行完整检查（Lexer → Parser → TypeChecker）
    /// </summary>
    /// <param name="targetPath">目标文件或目录路径</param>
    /// <returns>错误数量，0 表示检查通过</returns>
    public static int check(string? targetPath)
    {
        var path = targetPath ?? ".";
        var vFiles = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.v", SearchOption.AllDirectories)
            : File.Exists(path)
                ? [path]
                : [];

        var diagnostics = new DiagnosticSink();
        var lexer = new ValkyrieLexer(diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var typeChecker = new TypeChecker();
        var totalErrors = 0;

        foreach (var vFile in vFiles)
        {
            var source = File.ReadAllText(vFile);
            var tokens = lexer.tokenize(source);
            var ast = (CompilationUnit)parser.parse(tokens);

            if (diagnostics.messages.Count > 0)
            {
                totalErrors += diagnostics.messages.Count;
                diagnostics.clear();
                continue;
            }

            var typeResult = typeChecker.check(ast, vFile);
            totalErrors +=
                typeResult.diagnostics.Count(d => d.severity == DiagnosticSeverity.error);
            diagnostics.clear();
        }

        return totalErrors;
    }
}