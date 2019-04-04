namespace Std.Terminal.Components;

/// <summary>
///     格式化文本组件，支持样式化的多行文本渲染
/// </summary>
public sealed class TextBlock : IRenderable
{
    private readonly List<AnsiString> _lines;

    /// <summary>
    ///     初始�?<see cref="TextBlock" /> 的新实例�?    ///
    /// </summary>
    public TextBlock()
    {
        _lines = [];
        desired_width = 0;
    }

    /// <summary>
    ///     获取或设置文本样式名称（主题中的样式键）�?    ///
    /// </summary>
    public string? style_name { get; set; }

    /// <summary>
    ///     获取布局的期望宽度
    /// </summary>
    public int desired_width { get; private set; }

    /// <summary>
    ///     获取布局的期望高度
    /// </summary>
    public int desired_height => _lines.Count;

    /// <summary>
    ///     在指定区域内渲染此文本块�?    ///
    /// </summary>
    /// <param name="renderer">
    ///     终端渲染器�?/param>
    ///     <param name="region">渲染区域�?/param>
    public void render(AnsiTerminalRenderer renderer, Rectangle region)
    {
        for (var i = 0; i < _lines.Count && i < region.height; i++)
        {
            renderer.set_cursor_position(region.x, region.y + i);
            renderer.write(_lines[i].ansi_sequence);
        }
    }

    /// <summary>
    ///     使用指定文本创建 <see cref="TextBlock" />�?    ///
    /// </summary>
    /// <param name="text">
    ///     文本内容�?/param>
    ///     <returns>新的 <see cref="TextBlock" /> 实例�?/returns>
    public static TextBlock from(string text)
    {
        var block = new TextBlock();
        block.append_text(text);
        return block;
    }

    /// <summary>
    ///     追加一行文本�?    ///
    /// </summary>
    /// <param name="text">文本内容�?/param>
    public void append_text(string text)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd('\r');
            _lines.Add(AnsiString.plain(trimmed));
            if (trimmed.Length > desired_width) desired_width = trimmed.Length;
        }
    }

    /// <summary>
    ///     追加一行带样式的文本�?    ///
    /// </summary>
    /// <param name="text">ANSI 字符串�?/param>
    public void append_styled(AnsiString text)
    {
        _lines.Add(text);
        if (text.plain_text.Length > desired_width) desired_width = text.plain_text.Length;
    }
}