using System.Threading;
using Sonic.Console;
using Sonic.Terminal;
using Sonic.Command;
using Sonic.Command.Middleware;
using Sonic.Command.Ports;
using Sonic.Console;
using Sonic.Terminal;
using ICommand = Sonic.Command.ICommand;

namespace Sonic.Interactive;

/// <summary>
/// REPL 执行引擎，实现 Read → Eval → Print 主循环
/// </summary>
public sealed class ReplEngine
{
    private readonly IReadLineHandler _read_line_handler;
    private readonly ReplHistory _history;
    private readonly IOutputWriter _output;
    private readonly ICommandRouter _router;
    private readonly IMiddlewarePipeline _pipeline;
    private bool _running;

    /// <summary>
    /// 主提示符，默认为 "iris> "
    /// </summary>
    public string primary_prompt { get; set; } = "iris> ";

    /// <summary>
    /// 多行续行提示符，默认为 "....  "
    /// </summary>
    public string continuation_prompt { get; set; } = "....  ";

    /// <summary>
    /// 创建 REPL 引擎
    /// </summary>
    /// <param name="readLineHandler">行编辑处理器</param>
    /// <param name="output">输出端口</param>
    /// <param name="router">命令路由器</param>
    /// <param name="pipeline">中间件管道</param>
    /// <param name="history">命令历史</param>
    public ReplEngine(
        IReadLineHandler readLineHandler,
        IOutputWriter output,
        ICommandRouter router,
        IMiddlewarePipeline pipeline,
        ReplHistory? history = null)
    {
        _read_line_handler = readLineHandler;
        _output = output;
        _router = router;
        _pipeline = pipeline;
        _history = history ?? new ReplHistory();
    }

    /// <summary>
    /// 运行 REPL 主循环
    /// </summary>
    /// <param name="cancellation">取消令牌</param>
    public async Task run_loop(CancellationToken cancellation = default)
    {
        _running = true;

        _output.write_line($"Iris REPL v{GetType().Assembly.GetName().Version}");
        _output.write_line("输入 .help 查看帮助，.exit 退出");
        _output.write_line(string.Empty);

        while (_running && !cancellation.IsCancellationRequested)
        {
            var input = await _read_line_handler.read_line(primary_prompt, _history, cancellation);

            if (input is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (try_handle_builtin(input))
            {
                continue;
            }

            await evaluate(input, cancellation);
        }
    }

    /// <summary>
    /// 停止 REPL 循环
    /// </summary>
    public void stop()
    {
        _running = false;
    }

    private bool try_handle_builtin(string input)
    {
        var trimmed = input.Trim();

        switch (trimmed)
        {
            case ".exit":
            case ".quit":
                _running = false;
                return true;

            case ".help":
                show_help();
                return true;

            case ".clear":
                System.Console.Clear();
                return true;

            case ".history":
                show_history();
                return true;
        }

        if (trimmed.StartsWith(".describe ", StringComparison.OrdinalIgnoreCase))
        {
            var cmdName = trimmed[".describe ".Length..].Trim();
            show_command_info(cmdName);
            return true;
        }

        return false;
    }

    private void show_help()
    {
        _output.write_line("Iris REPL 内置命令：");
        _output.write_line("  .help               - 显示此帮助信息");
        _output.write_line("  .exit / .quit        - 退出 REPL");
        _output.write_line("  .clear               - 清屏");
        _output.write_line("  .history             - 显示命令历史");
        _output.write_line("  .describe <命令名>    - 显示命令详细信息");
        _output.write_line(string.Empty);
        _output.write_line("已注册的命令：");

        foreach (var info in _router.get_commands())
        {
            _output.write_line($"  {info.name,-20} {info.description}");
        }
    }

    private void show_history()
    {
        var index = 1;

        foreach (var entry in _history.entries)
        {
            _output.write_line($"  {index,4}  {entry}");
            index++;
        }
    }

    private void show_command_info(string commandName)
    {
        var commandInfo = _router.get_commands().FirstOrDefault(c => c.name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

        if (commandInfo is null)
        {
            _output.write_error($"未知命令: {commandName}");
            return;
        }

        _output.write_line($"命令: {commandInfo.name}");
        _output.write_line($"类型: {commandInfo.command_type.FullName}");
    }

    private async Task evaluate(string input, CancellationToken cancellation)
    {
        try
        {
            var commandType = _router.resolve_command(input);

            if (commandType is null)
            {
                _output.write_error($"未知命令: {input.Split(' ')[0]}");
                _output.write_line("输入 .help 查看可用命令");
                return;
            }

            var command = (ICommand)Activator.CreateInstance(commandType)!;
            var context = new CommandContext
            {
                cancellation_token = cancellation,
                output = _output,
                input = null
            };

            var exitCode = await _pipeline.execute(context, () => command.execute(context, cancellation), cancellation);

            if (exitCode != ExitCode.Success)
            {
                _output.write_error($"命令执行失败，退出码: {(int)exitCode}");
            }
        }
        catch (Exception ex)
        {
            _output.write_error($"执行时发生错误: {ex.Message}");
        }
    }
}
