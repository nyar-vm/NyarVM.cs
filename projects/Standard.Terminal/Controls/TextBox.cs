using Std.Command;

namespace Std.Terminal.Controls;

/// <summary>
///     文本输入框，支持单行输入和密码模式
/// </summary>
public sealed class TextBox : View
{
    /// <summary>
    ///     创建文本输入框
    /// </summary>
    public TextBox()
    {
        width = 20;
        height = 3;
        tab_stop = true;
    }

    /// <summary>
    ///     当前文本内容
    /// </summary>
    public string text { get; set; } = string.Empty;

    /// <summary>
    ///     占位符文本（可本地化）
    /// </summary>
    public LocalizableString placeholder { get; set; } = string.Empty;

    /// <summary>
    ///     最大输入长度
    /// </summary>
    public int max_length { get; set; } = int.MaxValue;

    /// <summary>
    ///     是否为密码输入模式
    /// </summary>
    public bool is_password { get; set; }

    /// <summary>
    ///     光标位置
    /// </summary>
    public int cursor_position { get; internal set; }

    /// <summary>
    ///     文本变化事件
    /// </summary>
    public event Action<TextBox, string>? OnTextChanged;

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var borderFg = is_focused ? new RgbColor(0, 150, 255) : RgbColor.Gray;
        var textFg = is_focused ? RgbColor.White : new RgbColor(200, 200, 200);

        if (!enabled)
        {
            borderFg = RgbColor.Gray;
            textFg = RgbColor.Gray;
        }

        ctx.draw_border(0, 0, width, height, BorderStyle.single, borderFg, ctx.default_background);

        var displayText = get_display_text();
        var innerWidth = width - 2;

        if (displayText.Length > innerWidth) displayText = displayText[^innerWidth..];

        ctx.draw_text(1, 1, displayText, textFg, ctx.default_background);

        if (is_focused)
        {
            var cursorX = 1 + System.Math.Min(cursor_position, innerWidth - 1);
            if (cursor_position >= displayText.Length) cursorX = 1 + displayText.Length;

            ctx.draw_text(cursorX, 1, " ", ctx.default_foreground, RgbColor.White);
        }
    }

    private string get_display_text()
    {
        if (string.IsNullOrEmpty(text))
        {
            var placeholder = this.placeholder.default_value;
            return string.IsNullOrEmpty(placeholder) ? string.Empty : placeholder;
        }

        if (is_password) return new string('*', text.Length);

        return text;
    }

    internal void insert_char(char c)
    {
        if (!enabled || text.Length >= max_length) return;

        text = text.Insert(cursor_position, c.ToString());
        cursor_position++;
        OnTextChanged?.Invoke(this, text);
    }

    internal void delete_char_before()
    {
        if (!enabled || cursor_position <= 0 || text.Length == 0) return;

        text = text.Remove(cursor_position - 1, 1);
        cursor_position--;
        OnTextChanged?.Invoke(this, text);
    }

    internal void move_cursor_left()
    {
        if (cursor_position > 0) cursor_position--;
    }

    internal void move_cursor_right()
    {
        if (cursor_position < text.Length) cursor_position++;
    }

    internal void move_cursor_home()
    {
        cursor_position = 0;
    }

    internal void move_cursor_end()
    {
        cursor_position = text.Length;
    }
}