using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Syntax;

/// <summary>
///     Valkyrie 源文本，封装 ISource 并集成 LineIndex 提供行列导航
/// </summary>
public sealed class ValkyrieSourceText : ISource
{
    private readonly ISource _source;

    /// <summary>
    ///     从字符串创建源文本
    /// </summary>
    public ValkyrieSourceText(string text)
    {
        _source = new StringSource(text);
        line_index = new LineIndex(_source);
    }

    /// <summary>
    ///     从 ISource 创建源文本
    /// </summary>
    public ValkyrieSourceText(ISource source)
    {
        _source = source;
        line_index = new LineIndex(_source);
    }

    /// <summary>
    ///     行索引
    /// </summary>
    public LineIndex line_index { get; }

    /// <summary>
    ///     总行数
    /// </summary>
    public int line_count => line_index.line_count;

    /// <inheritdoc />
    public char this[int index] => _source[index];

    /// <inheritdoc />
    public int length => _source.length;

    /// <inheritdoc />
    public string substring(Range range)
    {
        return _source.substring(range);
    }

    /// <summary>
    ///     根据偏移量获取行号和列号
    /// </summary>
    public (int Line, int Column) get_line_and_column(int offset)
    {
        return line_index.get_line_column(offset);
    }

    /// <summary>
    ///     根据行号和列号获取偏移量
    /// </summary>
    public int get_offset(int line, int column)
    {
        return line_index.get_offset(line, column);
    }

    /// <summary>
    ///     获取指定行的起始偏移量
    /// </summary>
    public int get_line_start(int line)
    {
        return line_index.get_line_start(line);
    }

    /// <summary>
    ///     获取指定行的结束偏移量（使用源文本长度修正 LineIndex 对最后一行的错误）
    /// </summary>
    public int get_line_end(int line)
    {
        var rawEnd = line_index.get_line_end(line);
        // LineIndex 对最后一行返回的是行首位置，修正为源文本长度
        if (rawEnd >= 0 && rawEnd < _source.length && line == line_index.line_count) return _source.length;

        return rawEnd;
    }

    /// <summary>
    ///     获取指定行的文本内容
    /// </summary>
    public string get_line_text(int line)
    {
        var start = get_line_start(line);
        var end = get_line_end(line);
        return _source.substring(new Range(start, end));
    }

    /// <summary>
    ///     根据 TextSpan 获取对应的源文本
    /// </summary>
    public string get_text(TextSpan span)
    {
        return _source.substring(new Range(span.start, span.end));
    }

    /// <summary>
    ///     根据偏移量获取 TextPosition
    /// </summary>
    public TextPosition get_position(int offset)
    {
        var (line, column) = get_line_and_column(offset);
        return new TextPosition(offset, line, column);
    }

    /// <summary>
    ///     根据 TextSpan 获取起始和结束的 TextPosition
    /// </summary>
    public (TextPosition Start, TextPosition End) get_span_position(TextSpan span)
    {
        return (get_position(span.start), get_position(span.end));
    }

    public override string ToString()
    {
        return _source.ToString() ?? string.Empty;
    }
}