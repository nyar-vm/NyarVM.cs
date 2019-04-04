using Std.Data.Text.Diagnostics;
using DiagnosticTextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Syntax;

public ref struct ParseContext<TLanguage, TContext>
    where TLanguage : Language
    where TContext : ISyntaxContext
{
    public ISource source { get; }
    public int position { get; set; }
    public TLanguage language { get; }
    public TContext context { get; }
    public DiagnosticSink diagnostics { get; }

    public ParseContext(ISource source, TLanguage language, TContext context, DiagnosticSink diagnostics)
    {
        this.source = source;
        position = 0;
        this.language = language;
        this.context = context;
        this.diagnostics = diagnostics;
    }

    public char current => position < source.length ? source[position] : '\0';

    public char peek(int offset = 0)
    {
        var index = position + offset;
        return index < source.length ? source[index] : '\0';
    }

    public void advance()
    {
        if (position < source.length) position++;
    }

    /// <summary>
    ///     跳过字符直到遇到目标字符
    /// </summary>
    public void skip_to(char target)
    {
        while (position < source.length)
        {
            if (source[position] == target) return;

            position++;
        }
    }

    /// <summary>
    ///     跳过字符直到遇到满足谓词的字符
    /// </summary>
    public void skip_to(Func<char, bool> predicate)
    {
        while (position < source.length)
        {
            if (predicate(source[position])) return;

            position++;
        }
    }

    /// <summary>
    ///     跳过字符直到遇到目标 NodeKind
    ///     仅为保持接口一致，实际不在此跳过
    /// </summary>
    public void skip_to(NodeKind kind)
    {
        while (position < source.length) position++;
    }

    /// <summary>
    ///     尝试执行解析，如果失败则报告错误并跳过
    /// </summary>
    public void recover(RefParseAction<TLanguage, TContext> parseAction, string message)
    {
        var errorCountBefore = diagnostics.get_diagnostics().Count(d => d.severity.is_error_level());
        var positionBefore = position;

        parseAction(ref this);

        if (diagnostics.get_diagnostics().Count(d => d.severity.is_error_level()) > errorCountBefore)
        {
            diagnostics.report_error(
                new DiagnosticTextSpan(positionBefore, System.Math.Max(1, position - positionBefore)),
                message);
            skip_to(c => c is ';' or '}' or '\n');
        }
    }

    /// <summary>
    ///     期望遇到目标字符，否则报告错误
    /// </summary>
    public bool expect(char expected, string code, string message)
    {
        if (current == expected) return true;

        diagnostics.report_error(default, message);
        return false;
    }

    /// <summary>
    ///     期望遇到满足谓词的字符，否则报告错误
    /// </summary>
    public bool expect(Func<char, bool> predicate, string code, string message)
    {
        if (predicate(current)) return true;

        diagnostics.report_error(default, message);
        return false;
    }

    public TextSpan get_span_from(int startPosition)
    {
        return default;
    }

    public string get_text(TextSpan span)
    {
        return source.substring(new Range(span.start, span.end));
    }
}
