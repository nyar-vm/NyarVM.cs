namespace Std.Terminal.Controls;

/// <summary>
///     滚动视图，支持平滑滚动动画和纵向滚动
/// </summary>
public sealed class ScrollView : View
{
    private int _scroll_duration_ms;
    private int _scroll_offset;
    private int _scroll_start_offset;
    private long _scroll_start_tick;
    private int _scroll_target;

    /// <summary>
    ///     创建滚动视图
    /// </summary>
    /// <param name="content">内部内容</param>
    public ScrollView(View content)
    {
        this.content = content;
        width = 10;
        height = 10;
    }

    /// <summary>
    ///     内部内容控件
    /// </summary>
    public View content { get; set; }

    /// <summary>
    ///     是否显示滚动条
    /// </summary>
    public bool show_scroll_bar { get; set; } = true;

    /// <summary>
    ///     当前滚动偏移
    /// </summary>
    public int scroll_offset
    {
        get => _scroll_offset;
        set
        {
            _scroll_offset = System.Math.Max(0, value);
            is_scrolling = false;
        }
    }

    /// <summary>
    ///     是否正在执行平滑滚动动画
    /// </summary>
    public bool is_scrolling { get; private set; }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var contentHeight = content.height;
        var innerWidth = show_scroll_bar ? width - 1 : width;
        var innerHeight = height;

        var maxScroll = System.Math.Max(0, contentHeight - innerHeight);

        if (is_scrolling)
        {
            var elapsed = (DateTime.UtcNow.Ticks - _scroll_start_tick) / TimeSpan.TicksPerMillisecond;
            var progress = System.Math.Clamp((float)elapsed / _scroll_duration_ms, 0f, 1f);
            var eased = ease_out_cubic(progress);
            _scroll_offset = (int)(_scroll_start_offset + (_scroll_target - _scroll_start_offset) * eased);

            if (progress >= 1f)
            {
                _scroll_offset = _scroll_target;
                is_scrolling = false;
            }
        }

        _scroll_offset = System.Math.Clamp(_scroll_offset, 0, maxScroll);

        var childCtx = ctx.create_child(0, -_scroll_offset, innerWidth, contentHeight);
        content.width = innerWidth;
        content.render(childCtx);

        if (show_scroll_bar && contentHeight > innerHeight)
        {
            var barHeight = System.Math.Max(1, (int)((float)innerHeight / contentHeight * innerHeight));
            var barPos = contentHeight > 0
                ? (int)((float)_scroll_offset / contentHeight * innerHeight)
                : 0;
            barPos = System.Math.Min(barPos, innerHeight - barHeight);

            for (var i = 0; i < innerHeight; i++)
                if (i >= barPos && i < barPos + barHeight)
                    ctx.draw_text(width - 1, i, "█", RgbColor.White, ctx.default_background);
                else
                    ctx.draw_text(width - 1, i, "░", RgbColor.Gray, ctx.default_background);
        }
    }

    /// <summary>
    ///     向上滚动
    /// </summary>
    /// <param name="lines">滚动行数</param>
    public void scroll_up(int lines = 1)
    {
        _scroll_offset = System.Math.Max(0, _scroll_offset - lines);
        is_scrolling = false;
    }

    /// <summary>
    ///     向下滚动
    /// </summary>
    /// <param name="lines">滚动行数</param>
    public void scroll_down(int lines = 1)
    {
        _scroll_offset += lines;
        is_scrolling = false;
    }

    /// <summary>
    ///     平滑滚动到指定行（带缓动动画）
    /// </summary>
    /// <param name="targetRow">目标行号</param>
    /// <param name="durationMs">动画时长（毫秒）</param>
    public void scroll_to(int targetRow, int durationMs = 200)
    {
        targetRow = System.Math.Max(0, targetRow);
        _scroll_start_offset = _scroll_offset;
        _scroll_target = targetRow;
        _scroll_duration_ms = System.Math.Max(1, durationMs);
        _scroll_start_tick = DateTime.UtcNow.Ticks;
        is_scrolling = true;
    }

    /// <summary>
    ///     平滑滚动到顶部
    /// </summary>
    /// <param name="durationMs">动画时长（毫秒）</param>
    public void scroll_to_top(int durationMs = 200)
    {
        scroll_to(0, durationMs);
    }

    /// <summary>
    ///     平滑滚动到底部
    /// </summary>
    /// <param name="durationMs">动画时长（毫秒）</param>
    public void scroll_to_bottom(int durationMs = 200)
    {
        var maxScroll = System.Math.Max(0, content.height - height);
        scroll_to(maxScroll, durationMs);
    }

    /// <summary>
    ///     停止当前动画（直接到达目标位置）
    /// </summary>
    public void stop_scroll()
    {
        if (is_scrolling)
        {
            _scroll_offset = _scroll_target;
            is_scrolling = false;
        }
    }

    private static float ease_out_cubic(float t)
    {
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }
}