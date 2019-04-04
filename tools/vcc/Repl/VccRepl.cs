using System.Text;
using Nyar.Types;
using Nyar.VM.NyarVM;
using Valkyrie.CLI.Compiler;
using ValueType = Nyar.Types.ValueType;

namespace Valkyrie.CLI.Repl;

/// <summary>
///     VCC 交互式 REPL
///     支持表达式求值、多行输入和会话状态维护
/// </summary>
public sealed class VccRepl
{
    private readonly VccCompiler _compiler;
    private readonly NyarVm _vm;
    private readonly StringBuilder _session_source;
    private bool _is_running;

    /// <summary>
    ///     初始化 VCC REPL
    /// </summary>
    public VccRepl()
    {
        _compiler = new VccCompiler();
        _vm = _compiler.vm;
        _session_source = new StringBuilder();
        _is_running = false;
    }

    /// <summary>
    ///     启动 REPL 交互循环
    /// </summary>
    public void run()
    {
        _is_running = true;

        Console.WriteLine("VCC REPL — Valkyrie Compiler Collection 交互式环境");
        Console.WriteLine("输入表达式进行求值，输入 exit 或 quit 退出");
        Console.WriteLine();

        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            while (_is_running)
            {
                Console.Write("vcc> ");
                var input = read_multi_line_input();

                if (input is null) continue;

                var trimmed = input.Trim();

                if (string.IsNullOrEmpty(trimmed)) continue;

                if (is_exit_command(trimmed)) break;

                evaluate_input(trimmed);
            }
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
    }

    #region 输入处理

    /// <summary>
    ///     读取多行输入，当大括号未匹配时自动续行
    /// </summary>
    /// <returns>完整输入内容，EOF 时返回 null</returns>
    private static string? read_multi_line_input()
    {
        var buffer = new StringBuilder();
        var braceDepth = 0;
        var isFirstLine = true;

        while (true)
        {
            var line = Console.ReadLine();

            if (line is null)
            {
                if (buffer.Length > 0) return buffer.ToString();

                return null;
            }

            buffer.AppendLine(line);

            foreach (var ch in line)
                if (ch == '{')
                    braceDepth++;
                else if (ch == '}') braceDepth--;

            if (braceDepth <= 0 && !isFirstLine) break;

            if (braceDepth > 0)
            {
                Console.Write("  ... ");
                isFirstLine = false;
            }
            else
            {
                break;
            }
        }

        return buffer.ToString();
    }

    /// <summary>
    ///     判断是否为退出命令
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <returns>是否为退出命令</returns>
    private static bool is_exit_command(string input)
    {
        return string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase)
               || string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     处理 Ctrl+C 中断信号
    /// </summary>
    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _is_running = false;
        Console.WriteLine();
    }

    #endregion

    #region 表达式求值

    /// <summary>
    ///     求值用户输入
    ///     将输入包装为 micro 函数，编译并在 NyarVM 中执行
    /// </summary>
    /// <param name="input">用户输入的表达式或语句</param>
    private void evaluate_input(string input)
    {
        try
        {
            throw new NotSupportedException(
                "REPL interactive evaluation is not yet supported with the new runtime API.");
        }
        catch (NyarRuntimeException ex)
        {
            Console.WriteLine($"  运行时错误：{ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  编译错误：{ex.Message}");
        }
    }

    /// <summary>
    ///     将用户输入包装为 micro 函数以便求值
    /// </summary>
    /// <param name="input">用户输入</param>
    /// <returns>包装后的完整源代码</returns>
    private static string wrap_as_micro_function(string input)
    {
        return $"micro __repl_eval() {{ {input} }}";
    }

    /// <summary>
    ///     格式化输出值
    /// </summary>
    /// <param name="value">Nyar 值</param>
    /// <returns>格式化后的字符串</returns>
    private static string format_value(Value value)
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
