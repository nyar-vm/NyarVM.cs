using Std.Data.Text.Syntax;

namespace Std.Data.Text.Markdown.Syntax;

/// <summary>
///     Markdown 语法高亮器
/// </summary>
public sealed class MarkdownSyntaxHighlighter
{
    /// <summary>
    ///     对 Markdown 源码进行语法高亮
    /// </summary>
    public IReadOnlyList<HighlightSpan> highlight(string source)
    {
        var lexer = new MarkdownLexer();
        var tokens = lexer.tokenize(source);
        var spans = new List<HighlightSpan>(tokens.Count);
        var offset = 0;

        foreach (var token in tokens)
        {
            spans.Add(new HighlightSpan
            {
                kind = HighlightKind.other,
                offset = offset,
                length = token.width
            });

            offset += token.width;
        }

        return spans;
    }
}