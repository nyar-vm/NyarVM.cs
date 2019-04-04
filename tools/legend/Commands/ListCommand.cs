using Core.Command;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.VM.LegacyVM.Runner;
using ICommand = Core.Command.ICommand;

namespace Legend.CLI.Commands;

/// <summary>
///     list 命令：列出支持的语言和目标
/// </summary>
[Command("list", "列出支持的语言和目标")]
public sealed class ListCommand : ICommand
{
    /// <summary>
    ///     语言名称到短标志的映射
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
    ///     执行 list 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        using var runner = new LegacyVmRunner();

        var languages = runner.language_service.get_all_languages();

        Console.WriteLine("支持的语言:");
        foreach (var lang in languages.OrderBy(l => l.name))
        {
            var flag = LanguageShortFlags.TryGetValue(lang.name, out var sf) ? $"(--{sf})" : "";
            Console.WriteLine($"  - {lang.name} {flag}");
        }

        Console.WriteLine();
        Console.WriteLine("支持的目标:");
        var distinctTargets = TargetAliases
            .GroupBy(kv => kv.Value)
            .Select(g => (canonical: g.Key, aliases: g.Select(kv => kv.Key).ToList()));
        foreach (var (canonical, aliases) in distinctTargets)
        {
            Console.WriteLine($"  - {canonical} (别名: {string.Join(", ", aliases)})");
        }

        Console.WriteLine();
        Console.WriteLine($"共 {languages.Count} 种语言, {distinctTargets.Count()} 个目标");
        return Task.FromResult(ExitCode.Success);
    }
}
