namespace Std.Terminal.Controls;

/// <summary>
///     复选框，表示布尔值状态
/// </summary>
public sealed class CheckBox : View
{
    private bool _is_checked;

    /// <summary>
    ///     创建复选框
    /// </summary>
    /// <param name="label">标签文本</param>
    public CheckBox(string label)
    {
        this.label = label;
        width = label.Length + 5;
        height = 1;
        tab_stop = true;
    }

    /// <summary>
    ///     是否选中
    /// </summary>
    public bool is_checked
    {
        get => _is_checked;
        set
        {
            _is_checked = value;
            OnCheckedChanged?.Invoke(this, value);
        }
    }

    /// <summary>
    ///     标签文本
    /// </summary>
    public string label { get; set; } = string.Empty;

    /// <summary>
    ///     选中状态变化事件
    /// </summary>
    public event Action<CheckBox, bool>? OnCheckedChanged;

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var checkChar = _is_checked ? 'x' : ' ';
        var checkText = is_focused ? $"[{checkChar}]" : $"[{checkChar}]";

        var fg = is_focused ? new RgbColor(100, 200, 255) : RgbColor.White;

        if (!enabled) fg = RgbColor.Gray;

        ctx.draw_text(0, 0, $"{checkText} {label}", fg, ctx.default_background);
    }

    /// <summary>
    ///     切换选中状态
    /// </summary>
    internal void toggle()
    {
        if (enabled) is_checked = !is_checked;
    }
}