using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.VM.LegacyVM.Compiler;
using Nyar.VM.LegacyVM.Runner;
using ICommand = Core.Command.ICommand;

namespace Legend.CLI.Commands;

/// <summary>
///     build 命令：编译脚本文件到指定目标
/// </summary>
[Command("build", "编译脚本文件到指定目标")]
public sealed class BuildCommand : ICommand
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
    ///     编译目标（native/nyar-vm/jvm/clr/wasm/wasi）
    /// </summary>
    [Option('t', "target", "编译目标（native/nyar-vm/jvm/clr/wasm/wasi）")]
    public string target { get; set; } = "nyar-vm";

    /// <summary>
    ///     执行 build 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"错误: 文件不存在: {filePath}");
            return Task.FromResult(ExitCode.Error);
        }

        var source = File.ReadAllText(filePath);
        var language = LegacyVmRunner.detect_language_from_file(filePath, source);
        var resolvedTarget = TargetAliases.GetValueOrDefault(target.ToLowerInvariant(), target);

        Console.WriteLine($"编译: {filePath}");
        Console.WriteLine($"语言: {language}");
        Console.WriteLine($"目标: {resolvedTarget}");

        using var runner = new LegacyVmRunner();

        try
        {
            if (resolvedTarget == "native")
            {
                var outputPath = Path.ChangeExtension(filePath, ".exe");
                runner.compile_to_pe("main", [], outputPath);
                Console.WriteLine($"输出: {outputPath}");
            }
            else if (resolvedTarget is "nyar-vm")
            {
                var compiler = runner.get_compiler(language);
                if (compiler == null)
                {
                    Console.Error.WriteLine($"错误: 语言 '{language}' 暂不支持编译到 NyarVM 字节码");
                    return Task.FromResult(ExitCode.Error);
                }

                var moduleName = Path.GetFileNameWithoutExtension(filePath);
                var module = compiler.compile(source, moduleName);
                var bytecodeCompiler = new NyarBytecodeCompiler();
                var bytecode = bytecodeCompiler.compile(module);
                var outputPath = Path.ChangeExtension(filePath, ".nyar");
                File.WriteAllBytes(outputPath, bytecode);
                Console.WriteLine($"输出: {outputPath} ({bytecode.Length} 字节)");
            }
            else
            {
                Console.WriteLine($"注意: 目标 '{resolvedTarget}' 的编译暂未实现");
                Console.WriteLine($"预期输出: {Path.ChangeExtension(filePath, get_extension_for_target(resolvedTarget))}");
            }

            return Task.FromResult(ExitCode.Success);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }
    }

    /// <summary>
    ///     根据目标类型返回对应的文件扩展名
    /// </summary>
    private static string get_extension_for_target(string target)
    {
        return target switch
        {
            "native" => ".exe",
            "nyar-vm" => ".nyar",
            "jvm" => ".class",
            "clr" => ".dll",
            "wasm" => ".wasm",
            "wasi" => ".wasm",
            _ => ".out"
        };
    }
}
