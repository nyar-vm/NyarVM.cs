using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

namespace Legion.CLI.Commands;

/// <summary>
///     legion spy 命令：内建诊断工具，用于反汇编和分析 WASM / JVM / CLR 产物及中间 IR
/// </summary>
[Command("spy", "内建诊断工具：反汇编与分析 WASM / JVM / CLR 产物及中间 IR")]
public sealed class LegionSpyCommand : ICommand
{
    /// <summary>
    ///     诊断模式：wasm / jvm / clr / lir / mir / verify
    /// </summary>
    [Argument(0, "诊断模式：wasm / jvm / clr / lir / mir / verify")]
    public string? mode { get; set; }

    /// <summary>
    ///     目标文件路径（wasm/jvm/clr 模式）或项目名（lir/mir/verify 模式）
    /// </summary>
    [Argument(1, "目标文件路径或项目名")]
    public string? target { get; set; }

    /// <summary>
    ///     函数索引或函数名（wasm/lir/mir 模式）
    /// </summary>
    [Option('f', "func", "函数索引或函数名")]
    public string? func { get; set; }

    /// <summary>
    ///     方法名（jvm/clr 模式）
    /// </summary>
    [Option('m', "method", "方法名")]
    public string? method { get; set; }

    /// <summary>
    ///     绝对偏移量（wasm 模式，用于定位验证错误）
    /// </summary>
    [Option('o', "offset", "绝对偏移量（wasm 模式）")]
    public string? offset { get; set; }

    /// <summary>
    ///     列出所有函数/方法
    /// </summary>
    [Option('l', "list", "列出所有函数/方法")]
    public bool list { get; set; }

    /// <summary>
    ///     错误点上下文行数（默认 20）
    /// </summary>
    [Option('c', "context", "错误点上下文行数（默认 20）")]
    public int context { get; set; } = 20;

    /// <summary>
    ///     编译目标（verify / lir 模式）：wasm / jvm / clr
    /// </summary>
    [Option('t', "target", "编译目标（verify / lir 模式）")]
    public string? target_platform { get; set; }

    /// <summary>
    ///     输出 JSON 格式
    /// </summary>
    [Option("json", "输出 JSON 格式")]
    public bool json { get; set; }

    /// <summary>
    ///     dump 函数体原始字节（wasm 模式，配合 --func 使用）
    /// </summary>
    [Option("hex", "dump 函数体原始字节（wasm 模式，配合 --func 使用）")]
    public bool hex { get; set; }

    /// <summary>
    ///     执行 spy 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        if (string.IsNullOrEmpty(mode))
        {
            print_help();
            return Task.FromResult(ExitCode.Error);
        }

        int? offsetValue = null;
        if (!string.IsNullOrEmpty(offset))
        {
            if (!int.TryParse(offset, out var parsed))
            {
                Console.Error.WriteLine($"错误：偏移量 '{offset}' 不是有效的整数");
                return Task.FromResult(ExitCode.Error);
            }
            offsetValue = parsed;
        }

        return mode.ToLowerInvariant() switch
        {
            "wasm" => LegionSpyWasm.run(target, func, offsetValue, list, this.context, json, hex),
            "jvm" => LegionSpyJvm.run(target, method, list, json),
            "clr" => LegionSpyClr.run(target, method, list, json),
            "lir" => LegionSpyLir.run(target, func, json, target_platform),
            "mir" => LegionSpyMir.run(target, func, json),
            "verify" => LegionSpyVerify.run(target, target_platform, this.context, json),
            _ => print_unknown_mode(mode),
        };
    }

    /// <summary>
    ///     打印帮助信息
    /// </summary>
    private static Task<ExitCode> print_help()
    {
        Console.WriteLine("用法：legion spy <mode> <target> [options]");
        Console.WriteLine();
        Console.WriteLine("模式：");
        Console.WriteLine("  wasm <file>          反汇编 WASM 二进制");
        Console.WriteLine("  jvm <jar|class>      反汇编 JVM 字节码");
        Console.WriteLine("  clr <exe|dll|msil>   dump MSIL");
        Console.WriteLine("  lir <project>        dump 指定函数的 LIR");
        Console.WriteLine("  mir <project>        dump 指定函数的 MIR");
        Console.WriteLine("  verify <project>     构建+验证+自动定位错误");
        Console.WriteLine();
        Console.WriteLine("通用选项：");
        Console.WriteLine("  --func <index|name>   函数索引或名称");
        Console.WriteLine("  --method <name>       方法名（jvm/clr）");
        Console.WriteLine("  --offset <abs>        绝对偏移量（wasm）");
        Console.WriteLine("  --list                列出所有函数/方法");
        Console.WriteLine("  --context <N>         上下文行数（默认 20）");
        Console.WriteLine("  --target <t>          编译目标（verify）");
        Console.WriteLine("  --json                JSON 输出");
        Console.WriteLine("  --hex                 dump 函数体原始字节（wasm）");
        return Task.FromResult(ExitCode.Error);
    }

    /// <summary>
    ///     打印未知模式错误
    /// </summary>
    private static Task<ExitCode> print_unknown_mode(string mode)
    {
        Console.Error.WriteLine($"错误：未知诊断模式 '{mode}'");
        Console.Error.WriteLine("可用模式：wasm / jvm / clr / lir / mir / verify");
        return Task.FromResult(ExitCode.Error);
    }
}
