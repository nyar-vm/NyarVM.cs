using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using Nyar.Types;
using Valkyrie.CLI.Compiler;
using ValueType = Nyar.Types.ValueType;
using ICommand = Core.Command.ICommand;

namespace Valkyrie.CLI.Commands;

/// <summary>
///     test 子命令：编译并运行测试
/// </summary>
[Command("test", "编译并运行测试")]
public sealed class VccTestCommand : ICommand
{
    /// <summary>
    ///     源文件路径
    /// </summary>
    [Argument(0, "源文件路径")]
    public string file { get; set; } = string.Empty;

    /// <summary>
    ///     编译目标（nyar / wasm / clr / jvm / native）
    /// </summary>
    [Option('t', "target", "编译目标（nyar / wasm / clr / jvm / native）")]
    public string target { get; set; } = "nyar";

    /// <summary>
    ///     详细输出
    /// </summary>
    [Option('v', "verbose", "详细输出")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 test 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"错误：源文件不存在 '{file}'");
            return Task.FromResult(ExitCode.Error);
        }

        var compiler = new VccCompiler();

        try
        {
            var result = compiler.compile_and_run(file, target, verbose);
            Console.WriteLine(verbose && result.type != ValueType.@null
                ? $"测试结果：{format_nyar_value(result)}"
                : "测试完成");
            return Task.FromResult(ExitCode.Success);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"测试失败：{ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }
    }

    #region 辅助方法

    /// <summary>
    ///     格式化 Nyar 值为可读字符串
    /// </summary>
    /// <param name="value">Nyar 值</param>
    /// <returns>格式化后的字符串</returns>
    private static string format_nyar_value(Value value)
    {
        return value.type switch
        {
            ValueType.i32 => value.i32.ToString(),
            ValueType.i64 => value.i64.ToString(),
            ValueType.f64 => value.f64.ToString(),
            ValueType.@bool => value.@bool.ToString(),
            ValueType.utf8 => value.utf8?.ToString() ?? "null",
            ValueType.@null => "null",
            _ => $"<{value.type}>"
        };
    }

    #endregion
}
