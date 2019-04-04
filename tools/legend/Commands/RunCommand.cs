using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.VM.LegacyVM.Runner;
using ICommand = Core.Command.ICommand;

namespace Legend.CLI.Commands;

/// <summary>
///     run 命令：运行脚本文件
/// </summary>
[Command("run", "运行脚本文件")]
public sealed class RunCommand : ICommand
{
    /// <summary>
    ///     目标别名到 Nyar 规范目标的映射
    /// </summary>
    private static readonly Dictionary<string, string> TargetAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pe"] = "native",
        ["exe"] = "native",
        ["native"] = "native",
        ["nyar"] = "nyar-vm",
        ["nyar-vm"] = "nyar-vm",
        ["nyar_vm"] = "nyarvm",
        ["nyarvm"] = "nyar-vm",
        ["jvm"] = "jvm",
        ["java"] = "jvm",
        ["clr"] = "clr",
        ["dotnet"] = "clr",
        ["net"] = "clr",
        ["wasm"] = "wasm",
        ["wasi"] = "wasi",
    };

    /// <summary>
    ///     脚本文件路径
    /// </summary>
    [Argument(0, "脚本文件路径", required = true)]
    public string filePath { get; set; } = string.Empty;

    /// <summary>
    ///     语言标识（留空自动检测文件扩展名）
    /// </summary>
    [Option('l', "language", "语言标识（留空自动检测文件扩展名）")]
    public string? language { get; set; }

    /// <summary>
    ///     执行目标（nyar-vm/native/jvm/clr/wasm/wasi）
    /// </summary>
    [Option('t', "target", "执行目标（nyar-vm/native/jvm/clr/wasm/wasi）")]
    public string target { get; set; } = "nyar-vm";

    /// <summary>
    ///     执行 run 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"错误: 文件不存在: {filePath}");
            return Task.FromResult(ExitCode.Error);
        }

        using var runner = new LegacyVmRunner();
        var source = File.ReadAllText(filePath);

        var resolvedLang = resolve_language(runner, language, source, filePath);
        if (resolvedLang is null)
        {
            Console.Error.WriteLine("错误：无法检测语言，请使用 --language 指定");
            return Task.FromResult(ExitCode.Error);
        }

        Console.WriteLine($"语言: {resolvedLang}");
        Console.WriteLine($"文件: {filePath}");
        Console.WriteLine("--- 输出 ---");

        try
        {
            var result = runner.run(resolvedLang, source);
            Console.WriteLine($"结果: {result}");
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
