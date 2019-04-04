using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.VM.LegacyVM.Runner;
using ICommand = Core.Command.ICommand;

namespace Legend.CLI.Commands;

/// <summary>
///     eval 命令：执行代码片段
/// </summary>
[Command("eval", "执行代码片段")]
public sealed class EvalCommand : ICommand
{
    /// <summary>
    ///     语言名称到短标志的映射（用于 --language 选项的别名解析）
    /// </summary>
    private static readonly Dictionary<string, string> LanguageShortFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["python"] = "py",
        ["javascript"] = "js",
        ["typescript"] = "ts",
        ["bash"] = "sh",
        ["powershell"] = "ps1",
        ["batch"] = "bat",
        ["rust"] = "rs",
        ["julia"] = "jl",
    };

    /// <summary>
    ///     要执行的代码
    /// </summary>
    [Argument(0, "要执行的代码", required = true)]
    public string code { get; set; } = string.Empty;

    /// <summary>
    ///     语言标识（如 js/py/rust/c/wasm/ts/sh/ps1。留空自动检测）
    /// </summary>
    [Option('l', "language", "语言标识（如 js/py/rust/c/wasm/ts/sh/ps1。留空自动检测）")]
    public string? language { get; set; }

    /// <summary>
    ///     执行 eval 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        using var runner = new LegacyVmRunner();

        var resolvedLang = resolve_language(runner, language, code);
        if (resolvedLang is null)
        {
            Console.Error.WriteLine("错误：无法检测语言，请使用 --language 指定");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"自动检测语言: {resolvedLang}");

        try
        {
            var result = runner.run(resolvedLang, code);
            Console.WriteLine(result);
            return Task.FromResult(ExitCode.Success);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }
    }

    /// <summary>
    ///     解析语言：优先使用显式指定的语言，否则自动检测
    /// </summary>
    private static string? resolve_language(LegacyVmRunner runner, string? explicitLanguage, string source,
        string? filePath = null)
    {
        if (!string.IsNullOrEmpty(explicitLanguage))
        {
            return explicitLanguage;
        }

        if (filePath is not null)
        {
            return LegacyVmRunner.detect_language_from_file(filePath, source);
        }

        return LegacyVmRunner.detect_language_from_content(source);
    }
}
