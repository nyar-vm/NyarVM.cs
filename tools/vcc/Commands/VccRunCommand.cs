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
///     run 子命令：编译并运行源文件
/// </summary>
[Command("run", "编译并运行源文件")]
public sealed class VccRunCommand : ICommand
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
    ///     直接运行 .nyar 字节码文件
    /// </summary>
    [Option('n', "nyar", "直接运行 .nyar 字节码文件")]
    public bool nyar { get; set; }

    /// <summary>
    ///     指定入口函数名（默认 main）
    /// </summary>
    [Option('f', "function", "指定入口函数名（默认 main）")]
    public string? function { get; set; }

    /// <summary>
    ///     详细输出
    /// </summary>
    [Option('v', "verbose", "详细输出")]
    public bool verbose { get; set; }

    /// <summary>
    ///     执行 run 命令
    /// </summary>
    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        var compiler = new VccCompiler();

        if (nyar || file.EndsWith(".nyar", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var entryFunction = string.IsNullOrWhiteSpace(function) ? "main" : function;
                var result = compiler.run_nyar_file(file, entryFunction, verbose);
                if (result.type != ValueType.@null)
                {
                    Console.WriteLine(format_nyar_value(result));
                }

                return Task.FromResult(ExitCode.Success);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"错误：{ex.Message}");
                return Task.FromResult(ExitCode.Error);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"运行时错误：{ex.Message}");
                return Task.FromResult(ExitCode.Error);
            }
        }

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"错误：源文件不存在 '{file}'");
            return Task.FromResult(ExitCode.Error);
        }

        try
        {
            var result = compiler.compile_and_run(file, target, verbose);
            if (result.type != ValueType.@null)
            {
                Console.WriteLine(format_nyar_value(result));
            }

            return Task.FromResult(ExitCode.Success);
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"错误：{ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"错误：{ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"编译失败：{ex.Message}");
            return Task.FromResult(ExitCode.Error);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"运行时错误：{ex.Message}");
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
