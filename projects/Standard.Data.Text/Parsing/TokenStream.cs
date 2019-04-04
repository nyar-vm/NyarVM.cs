using Std.Data.Text.Diagnostics;
using Std.Data.Text.Syntax;
using DiagnosticTextSpan = Std.Text.TextSpan;

namespace Std.Data.Text.Parsing;

/// <summary>
///     Parser 的 Token 源，封装 GreenLeafNode 列表的遍历操作
/// </summary>
public class TokenStream
{
    private const string _parser_error_code = "PARSE";
    private const string _default_file_path = "";

    private readonly IReadOnlyList<GreenLeafNode> _tokens;

    /// <summary>
    ///     用 Token 列表和分类器初始化
    /// </summary>
    public TokenStream(IReadOnlyList<GreenLeafNode> tokens, INodeKindClassifier classifier,
        DiagnosticSink? diagnostics = null)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        this.classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        this.diagnostics = diagnostics;
        position = 0;
    }

    /// <summary>
    ///     诊断接收器
    /// </summary>
    public DiagnosticSink? diagnostics { get; }

    /// <summary>
    ///     当前使用的 NodeKind 分类器
    /// </summary>
    public INodeKindClassifier classifier { get; }

    /// <summary>
    ///     获取当前 Token
    /// </summary>
    public GreenLeafNode current => position < _tokens.Count ? _tokens[position] : _tokens[^1];

    /// <summary>
    ///     当前在源中的位置
    /// </summary>
    public int position { get; private set; }

    /// <summary>
    ///     是否已到达 Token 流末尾（跳过 Eos 后视为结束）
    /// </summary>
    public bool is_at_end()
    {
        return position >= _tokens.Count || peek().kind == classifier.end_of_stream_kind;
    }

    /// <summary>
    ///     向前看指定偏移量的 Token（0 = 当前）
    /// </summary>
    public GreenLeafNode peek(int offset = 0)
    {
        var index = position + offset;
        if (index >= _tokens.Count) return _tokens[^1];

        return _tokens[index];
    }

    /// <summary>
    ///     向前看指定偏移量 Token 的文本
    /// </summary>
    public string peek_text(int offset = 0)
    {
        return peek(offset).text ?? string.Empty;
    }

    /// <summary>
    ///     向前看指定偏移量 Token 的 NodeKind
    /// </summary>
    public NodeKind peek_kind(int offset = 0)
    {
        return peek(offset).kind;
    }

    /// <summary>
    ///     检查当前 Token 的 Kind 是否匹配
    /// </summary>
    public bool check(NodeKind kind)
    {
        if (is_at_end()) return false;

        return current.kind == kind;
    }

    /// <summary>
    ///     检查指定偏移的 Token 的 Kind 是否匹配
    /// </summary>
    public bool check(NodeKind kind, int offset)
    {
        return peek(offset).kind == kind;
    }

    /// <summary>
    ///     检查当前 Token 的文本是否匹配（忽略大小写）
    /// </summary>
    public bool check(string text)
    {
        if (is_at_end()) return false;

        return string.Equals(current.text, text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     检查当前 Token 是否为关键词
    /// </summary>
    public bool is_keyword()
    {
        return classifier.is_keyword(current.kind);
    }

    /// <summary>
    ///     检查当前 Token 是否为操作符
    /// </summary>
    public bool is_operator()
    {
        return classifier.is_operator(current.kind);
    }

    /// <summary>
    ///     检查指定偏移的 Token 是否为操作符
    /// </summary>
    public bool is_operator(int offset)
    {
        return classifier.is_operator(peek(offset).kind);
    }

    /// <summary>
    ///     检查当前 Token 是否为字面量
    /// </summary>
    public bool is_literal()
    {
        return classifier.is_literal(current.kind);
    }

    /// <summary>
    ///     消耗当前 Token 并前进一步，返回消耗的 Token
    /// </summary>
    public GreenLeafNode advance()
    {
        if (!is_at_end()) position++;

        return _tokens[position - 1];
    }

    /// <summary>
    ///     消耗当前 Token 并前进一步，返回消耗 Token 的文本
    /// </summary>
    public string advance_text()
    {
        return advance().text ?? string.Empty;
    }

    /// <summary>
    ///     如果当前 Kind 匹配则消耗它并返回 true，否则返回 false
    /// </summary>
    public bool match(NodeKind kind)
    {
        if (check(kind))
        {
            advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     如果当前 Token 文本匹配则消耗它并返回 true，否则返回 false
    /// </summary>
    public bool match(string text)
    {
        if (check(text))
        {
            advance();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     如果当前 Kind 匹配则消耗它并返回 true，否则返回 false
    /// </summary>
    public bool match(NodeKind kind, out GreenLeafNode token)
    {
        if (check(kind))
        {
            token = advance();
            return true;
        }

        token = default!;
        return false;
    }

    /// <summary>
    ///     期望当前 Token 是指定的 Kind，否则报告诊断并抛出异常
    /// </summary>
    public GreenLeafNode expect(NodeKind kind)
    {
        if (check(kind)) return advance();

        var message = $"期望 Token 类型 {kind}，但遇到 {current.kind} (\"{current.text}\")，位置 {position}";
        diagnostics?.report_error(new DiagnosticTextSpan(position, 1), message);
        throw new InvalidOperationException(message);
    }

    /// <summary>
    ///     期望当前 Token 是 Keyword 且有指定文本，否则报告诊断并抛出异常
    /// </summary>
    public GreenLeafNode expect_keyword(string keyword)
    {
        if (is_keyword() && check(keyword)) return advance();

        var message = $"期望关键字 \"{keyword}\"，但遇到 \"{current.text}\"，位置 {position}";
        diagnostics?.report_error(new DiagnosticTextSpan(position, 1), message);
        throw new InvalidOperationException(message);
    }

    /// <summary>
    ///     同步到下一个语句边界（错误恢复用）
    /// </summary>
    public void synchronize()
    {
        var statementEndKind = classifier.statement_end_kind;
        var blockOpenKind = classifier.block_open_kind;
        var blockCloseKind = classifier.block_close_kind;
        advance();

        while (!is_at_end())
        {
            if (_tokens[position - 1].kind == statementEndKind) return;

            var curKind = peek().kind;
            if (classifier.is_keyword(curKind)
                || curKind == blockOpenKind
                || curKind == blockCloseKind)
                return;

            advance();
        }
    }

    /// <summary>
    ///     从解析错误中恢复：报告错误并同步到下一个语句边界
    /// </summary>
    public void recover_from_error(string message)
    {
        diagnostics?.report_error(new DiagnosticTextSpan(position, 1), message);
        synchronize();
    }
}
