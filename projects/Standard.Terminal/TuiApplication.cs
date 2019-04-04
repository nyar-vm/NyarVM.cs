using Core.Terminal;
using Std.Command;
using Std.Command.Ports;
using Std.Terminal.Controls;
using Std.Terminal.Layout;

namespace Std.Terminal;

/// <summary>
///     Iris TUI 应用程序入口，实现 IInteractiveShell 以便统一宿主调度
/// </summary>
public sealed class TuiApplication : IInteractiveShell, IDisposable
{
    private readonly TuiConfig _config;
    private readonly List<View> _tab_stop_controls = [];
    private ScreenBuffer? _current_buffer;
    private CancellationTokenSource? _input_cts;
    private CancellationTokenSource? _render_cts;
    private DifferentialRenderer? _renderer;
    private View? _root_view;
    private volatile bool _running;
    private volatile ShellState _state = ShellState.idle;

    /// <summary>
    ///     创建 TUI 应用程序
    /// </summary>
    /// <param name="config">应用程序配置</param>
    public TuiApplication(TuiConfig config)
    {
        _config = config;
    }

    /// <summary>
    ///     使用配置和根视图创建并启用 TUI 应用
    /// </summary>
    /// <param name="config">配置信息</param>
    /// <param name="root">根视图</param>
    public TuiApplication(TuiConfig config, View root) : this(config)
    {
        set_content_view(root);
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_running) stop();

        _render_cts?.Dispose();
        _input_cts?.Dispose();
    }

    /// <inheritdoc />
    public string name => _config.title;

    /// <inheritdoc />
    public ShellState state
    {
        get => _state;
        private set
        {
            if (_state == value) return;

            var oldState = _state;
            _state = value;
            StateChanged?.Invoke(oldState, value);
        }
    }

    /// <inheritdoc />
    public event Action<ShellState, ShellState>? StateChanged;

    /// <inheritdoc />
    public async Task<ExitCode> run(CancellationToken cancellation = default)
    {
        if (_root_view == null) return ExitCode.Error;

        state = ShellState.starting;

        _running = true;
        _renderer = new DifferentialRenderer();
        _render_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        _input_cts = new CancellationTokenSource();

        if (!System.Console.IsInputRedirected)
        {
            System.Console.CursorVisible = false;
            System.Console.TreatControlCAsInput = true;
        }

        state = ShellState.running;

        var renderTask = render_loop(_render_cts.Token);
        var inputTask = input_loop(_input_cts.Token);

        var completedTask = await Task.WhenAny(renderTask, inputTask);

        await shutdown();

        state = ShellState.stopping;
        state = ShellState.stopped;

        return ExitCode.Success;
    }

    /// <inheritdoc />
    public Task stop()
    {
        stop_internal();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     设置内容根视图
    /// </summary>
    /// <param name="root">根视图</param>
    public void set_content_view(View root)
    {
        _root_view = root;
        collect_tab_stop_controls(root);
    }

    /// <summary>
    ///     启动 TUI 并阻塞直到结束
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task run()
    {
        await run(CancellationToken.None);
    }

    private async Task render_loop(CancellationToken ct)
    {
        var frameInterval = TimeSpan.FromMilliseconds(1000.0 / _config.frame_rate);

        while (!ct.IsCancellationRequested)
        {
            var frameStart = DateTime.UtcNow;

            render_frame();

            var elapsed = DateTime.UtcNow - frameStart;
            var delay = frameInterval - elapsed;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
        }
    }

    internal void render_frame()
    {
        if (_root_view == null) return;

        _renderer ??= new DifferentialRenderer();

        var width = 80;
        var height = 25;

        try
        {
            if (System.Console.WindowWidth > 0) width = System.Console.WindowWidth;

            if (System.Console.WindowHeight > 0) height = System.Console.WindowHeight;
        }
        catch (IOException)
        {
        }

        if (_current_buffer == null || _current_buffer.width != width || _current_buffer.height != height)
            _current_buffer = new ScreenBuffer(width, height);
        else
            _current_buffer.clear();

        _root_view.width = width;
        _root_view.height = height;

        var ctx = new RenderContext(_current_buffer, width, height, 0, 0);
        _root_view.render(ctx);

        _renderer!.render(_current_buffer);
    }

    private async Task input_loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _running)
        {
            if (!System.Console.KeyAvailable)
            {
                await Task.Delay(16, ct);
                continue;
            }

            var key = System.Console.ReadKey(true);

            var focused = find_focused_control();
            if (focused != null)
            {
                var handled = InputEventManager.dispatch_key(focused, _tab_stop_controls, key);
                if (handled) continue;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                stop();
                return;
            }
        }
    }

    private View? find_focused_control()
    {
        return _tab_stop_controls.FirstOrDefault(c => c.is_focused);
    }

    private void collect_tab_stop_controls(View root)
    {
        _tab_stop_controls.Clear();
        collect_controls(root);

        if (_tab_stop_controls.Count > 0) _tab_stop_controls[0].set_focused(true);
    }

    private void collect_controls(View view)
    {
        if (view is { visible: true, tab_stop: true }) _tab_stop_controls.Add(view);

        if (view is Panel { content: not null } panel) collect_controls(panel.content);

        if (view is Window { content: not null } window) collect_controls(window.content);

        if (view is VBox vbox)
            foreach (var child in vbox.children)
                collect_controls(child);

        if (view is HBox hbox)
            foreach (var child in hbox.children)
                collect_controls(child);

        if (view is ScrollView { content: not null } scroll) collect_controls(scroll.content);
    }

    /// <summary>
    ///     停止 TUI 应用程序
    /// </summary>
    private void stop_internal()
    {
        _running = false;
        _render_cts?.Cancel();
        _input_cts?.Cancel();
    }

    /// <summary>
    ///     获取所有 Tab 可切换控件
    /// </summary>
    public IReadOnlyList<View> get_tab_stop_controls()
    {
        return _tab_stop_controls;
    }

    private async Task shutdown()
    {
        _running = false;
        await _render_cts?.CancelAsync();
        await _input_cts?.CancelAsync();

        if (!System.Console.IsInputRedirected)
        {
            System.Console.CursorVisible = true;
            System.Console.TreatControlCAsInput = false;
        }

        _render_cts?.Dispose();
        _input_cts?.Dispose();
        _render_cts = null;
        _input_cts = null;
    }
}