using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Lexing;

/// <summary>
///     词法分析器基类，提供通用扫描逻辑，产出 GreenLeafNode。
///     仅追踪字符偏移量（Position），不追踪行列。
///     行列信息由 LineIndex 根据 offset 按需计算。
/// </summary>
public abstract class LexerBase
{
    protected int _column = 1;
    protected DiagnosticSink? _diagnostics;
    protected int _line = 1;
    protected int _position;
    protected ISource _source = StringSource.empty;

    protected LexerBase()
    {
    }

    protected LexerBase(ISource source, DiagnosticSink? diagnostics = null)
    {
        _source = source;
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     执行词法分析，产出 GreenLeafNode 列表
    /// </summary>
    public abstract IReadOnlyList<GreenLeafNode> tokenize(string source);

    /// <summary>
    ///     是否已到达源代码末尾
    /// </summary>
    protected bool is_at_end()
    {
        return _position >= _source.length;
    }

    /// <summary>
    ///     查看当前字符但不移动位置
    /// </summary>
    protected char peek()
    {
        return is_at_end() ? '\0' : _source[_position];
    }

    /// <summary>
    ///     查看指定偏移处的字符
    /// </summary>
    protected char peek(int offset)
    {
        var index = _position + offset;
        return index >= _source.length ? '\0' : _source[index];
    }

    /// <summary>
    ///     查看下一个字符
    /// </summary>
    protected char peek_next()
    {
        return peek(1);
    }

    /// <summary>
    ///     前进一个字符并返回
    /// </summary>
    protected char advance()
    {
        var c = _source[_position];
        _position++;
        return c;
    }

    /// <summary>
    ///     尝试匹配指定字符
    /// </summary>
    protected bool match(char expected)
    {
        if (is_at_end() || _source[_position] != expected) return false;

        advance();
        return true;
    }

    /// <summary>
    ///     跳过空白字符
    /// </summary>
    protected void skip_whitespace()
    {
        while (!is_at_end() && char.IsWhiteSpace(peek())) advance();
    }

    /// <summary>
    ///     从 ISource 执行词法分析
    /// </summary>
    public virtual IReadOnlyList<GreenLeafNode> tokenize(ISource source)
    {
        _source = source;
        reset();
        return tokenize(source.substring(new Range(0, source.length)));
    }

    /// <summary>
    ///     重置内部状态
    /// </summary>
    protected void reset()
    {
        _position = 0;
    }
}