using Sonic.Interactive;
using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// 状态行上下文，管理状态行的生命周期，支持实时刷新和 Spinner 动画
/// </summary>
public sealed class StatusContext : IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _render_task;
    private int _cursor_line;
    private bool _is_running;
    private string _final_status = string.Empty;
    private bool _completed;

    /// <summary>
    /// 当前状态文本
    /// </summary>
    public string status { get; set; } = string.Empty;

    /// <summary>
    /// Spinner 样式
    /// </summary>
    public SpinnerStyle spinner_style { get; set; } = SpinnerStyle.dots;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool is_running => _is_running;

    /// <summary>
    /// 启动状态行并执行异步操作
    /// </summary>
    /// <param name="action">异步操作，在其中更新 Status 属性</param>
    public async Task run(Func<StatusContext, Task> action)
    {
        if (TerminalCapability.is_output_redirected)
        {
            System.Console.WriteLine($"  ⏳ {status}");
            await action(this);
            System.Console.WriteLine($"  ✓ {status}");
            return;
        }

        start_animation();

        try
        {
            await action(this);
        }
        finally
        {
            await stop_animation();
        }

        if (!_completed)
        {
            success();
        }
    }

    /// <summary>
    /// 启动状态行动画
    /// </summary>
    public void start_animation()
    {
        if (_is_running)
        {
            return;
        }

        var useAnsi = TerminalCapability.should_use_ansi;
        var frames = get_spinner_frames(spinner_style);
        var frameIndex = 0;

        try
        {
            _cursor_line = System.Console.CursorTop;
        }
        catch (IOException)
        {
            _is_running = true;
            return;
        }

        _cts = new CancellationTokenSource();
        _render_task = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var frame = frames[frameIndex % frames.Length];
                    System.Console.SetCursorPosition(0, _cursor_line);
                    System.Console.Write(AnsiStyling.clear_line());
                    System.Console.SetCursorPosition(0, _cursor_line);

                    if (useAnsi)
                    {
                        System.Console.Write($"  {AnsiStyling.cyan(frame)}  {status}");
                    }
                    else
                    {
                        System.Console.Write($"  {frame}  {status}");
                    }

                    frameIndex++;
                    await Task.Delay(80, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    break;
                }
            }
        }, _cts.Token);

        _is_running = true;
    }

    /// <summary>
    /// 停止状态行动画
    /// </summary>
    public async Task stop_animation()
    {
        if (!_is_running)
        {
            return;
        }

        _is_running = false;

        if (_cts != null)
        {
            _cts.Cancel();
            try
            {
                if (_render_task != null)
                {
                    await _render_task;
                }
            }
            catch (OperationCanceledException)
            {
            }

            _cts.Dispose();
            _cts = null;
            _render_task = null;
        }
    }

    /// <summary>
    /// 显示成功状态
    /// </summary>
    /// <param name="message">成功消息（为 null 时使用当前 Status）</param>
    public void success(string? message = null)
    {
        _completed = true;
        _final_status = message ?? status;

        if (TerminalCapability.should_use_ansi)
        {
            System.Console.WriteLine($"\r  {AnsiStyling.green("✓")}  {_final_status}    ");
        }
        else
        {
            System.Console.WriteLine($"\r  ✓  {_final_status}    ");
        }
    }

    /// <summary>
    /// 显示警告状态
    /// </summary>
    /// <param name="message">警告消息</param>
    public void warning(string? message = null)
    {
        _completed = true;
        _final_status = message ?? status;

        if (TerminalCapability.should_use_ansi)
        {
            System.Console.WriteLine($"\r  {AnsiStyling.yellow("⚠")}  {_final_status}    ");
        }
        else
        {
            System.Console.WriteLine($"\r  ⚠  {_final_status}    ");
        }
    }

    /// <summary>
    /// 显示失败状态
    /// </summary>
    /// <param name="message">失败消息</param>
    public void fail(string? message = null)
    {
        _completed = true;
        _final_status = message ?? status;

        if (TerminalCapability.should_use_ansi)
        {
            System.Console.WriteLine($"\r  {AnsiStyling.red("✗")}  {_final_status}    ");
        }
        else
        {
            System.Console.WriteLine($"\r  ✗  {_final_status}    ");
        }
    }

    /// <summary>
    /// 异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await stop_animation();
    }

    private static string[] get_spinner_frames(SpinnerStyle style)
    {
        return style switch
        {
            SpinnerStyle.dots => ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"],
            SpinnerStyle.line => ["|", "/", "-", "\\"],
            SpinnerStyle.dots2 => ["⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"],
            SpinnerStyle.arc => ["◜", "◝", "◞", "◟"],
            SpinnerStyle.moon => ["🌑", "🌒", "🌓", "🌔", "🌕", "🌖", "🌗", "🌘"],
            SpinnerStyle.arrow => ["←", "↖", "↑", "↗", "→", "↘", "↓", "↙"],
            _ => ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]
        };
    }
}