using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// Spinner 上下文，管理等待动画的生命周期
/// </summary>
public sealed class SpinnerContext : IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _spinner_task;
    private int _cursor_top;
    private bool _is_running;

    /// <summary>
    /// Spinner 消息文本
    /// </summary>
    public string message { get; set; }

    /// <summary>
    /// Spinner 样式类型
    /// </summary>
    public SpinnerStyle style { get; set; } = SpinnerStyle.dots;

    /// <summary>
    /// 是否在完成后清除行（默认保留成功消息）
    /// </summary>
    public bool clear_on_complete { get; set; }

    /// <summary>
    /// 帧间隔（毫秒），默认 80ms
    /// </summary>
    public int frame_interval_ms { get; set; } = 80;

    /// <summary>
    /// 当前是否正在运行
    /// </summary>
    public bool is_running => _is_running;

    /// <summary>
    /// 创建 Spinner 上下文
    /// </summary>
    /// <param name="message">消息文本</param>
    public SpinnerContext(string message)
    {
        this.message = message;
    }

    /// <summary>
    /// 启动 Spinner 动画并执行异步操作
    /// </summary>
    /// <typeparam name="T">操作返回值类型</typeparam>
    /// <param name="action">返回结果的异步操作</param>
    /// <returns>操作结果</returns>
    public async Task<T> start<T>(Func<SpinnerContext, Task<T>> action)
    {
        start_animation();

        try
        {
            var result = await action(this);
            return result;
        }
        finally
        {
            await stop_animation();
        }
    }

    /// <summary>
    /// 启动 Spinner 动画并执行无返回值的异步操作
    /// </summary>
    /// <param name="action">异步操作</param>
    public async Task start(Func<SpinnerContext, Task> action)
    {
        await start<bool>(async ctx =>
        {
            await action(ctx);
            return true;
        });
    }

    /// <summary>
    /// 仅启动动画（不包裹操作），需手动调用 StopAnimationAsync 或 DisposeAsync
    /// </summary>
    public void start_animation()
    {
        if (_is_running)
        {
            return;
        }

        if (TerminalCapability.is_output_redirected)
        {
            System.Console.WriteLine($"  ⏳ {message}");
            _is_running = true;
            return;
        }

        var frames = get_frames(style);
        var frameIndex = 0;
        var useAnsi = TerminalCapability.should_use_ansi;

        try
        {
            _cursor_top = System.Console.CursorTop;
        }
        catch (IOException)
        {
            _is_running = true;
            return;
        }

        _cts = new CancellationTokenSource();
        _spinner_task = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var frame = frames[frameIndex % frames.Length];
                    System.Console.SetCursorPosition(0, _cursor_top);
                    System.Console.Write(AnsiStyling.clear_line());
                    System.Console.SetCursorPosition(0, _cursor_top);

                    if (useAnsi)
                    {
                        System.Console.Write($"  {AnsiStyling.cyan(frame)}  {message}");
                    }
                    else
                    {
                        System.Console.Write($"  {frame}  {message}");
                    }

                    frameIndex++;
                    await Task.Delay(frame_interval_ms, _cts.Token);
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
    /// 停止动画（不显示任何完成状态消息）
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
                if (_spinner_task != null)
                {
                    await _spinner_task;
                }
            }
            catch (OperationCanceledException)
            {
            }

            _cts.Dispose();
            _cts = null;
            _spinner_task = null;
        }

        if (!TerminalCapability.is_output_redirected)
        {
            try
            {
                System.Console.Write($"\r{AnsiStyling.clear_line()}");
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>
    /// 停止动画并显示成功消息
    /// </summary>
    /// <param name="message">成功消息（为 null 时使用 Message）</param>
    public async Task stop_with_success(string? message = null)
    {
        await stop_animation();
        success(message);
    }

    /// <summary>
    /// 停止动画并显示失败消息
    /// </summary>
    /// <param name="message">失败消息</param>
    public async Task stop_with_fail(string? message = null)
    {
        await stop_animation();
        fail(message);
    }

    /// <summary>
    /// 显示成功消息并清除 Spinner 残留
    /// </summary>
    /// <param name="message">成功消息（为 null 时使用 Message）</param>
    public void success(string? message = null)
    {
        var text = message ?? this.message;

        if (TerminalCapability.is_output_redirected)
        {
            System.Console.WriteLine($"  ✓ {text}");
            return;
        }

        if (clear_on_complete)
        {
            try
            {
                System.Console.Write($"\r{AnsiStyling.clear_line()}");
            }
            catch (IOException)
            {
            }

            return;
        }

        if (TerminalCapability.should_use_ansi)
        {
            System.Console.WriteLine($"\r  {AnsiStyling.green("✓")}  {text}    ");
        }
        else
        {
            System.Console.WriteLine($"\r  ✓  {text}    ");
        }
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    /// <param name="message">警告消息</param>
    public void warning(string? message = null)
    {
        var text = message ?? this.message;

        if (TerminalCapability.should_use_ansi)
        {
            System.Console.WriteLine($"\r  {AnsiStyling.yellow("⚠")}  {text}    ");
        }
        else
        {
            System.Console.WriteLine($"\r  ⚠  {text}    ");
        }
    }

    /// <summary>
    /// 显示失败消息
    /// </summary>
    /// <param name="message">失败消息</param>
    public void fail(string? message = null)
    {
        var text = message ?? this.message;

        if (TerminalCapability.should_use_ansi)
        {
            System.Console.WriteLine($"\r  {AnsiStyling.red("✗")}  {text}    ");
        }
        else
        {
            System.Console.WriteLine($"\r  ✗  {text}    ");
        }
    }

    /// <summary>
    /// 异步释放资源，停止动画
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await stop_animation();
    }

    private static string[] get_frames(SpinnerStyle style)
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