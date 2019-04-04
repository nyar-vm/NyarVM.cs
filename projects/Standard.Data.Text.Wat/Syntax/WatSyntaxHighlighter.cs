using Std.Data.Text.Syntax;

namespace Oak.Wat.Syntax;

/// <summary>
///     WAT（WebAssembly Text）语法高亮器
/// </summary>
public sealed class WatSyntaxHighlighter
{
    /// <summary>
    ///     对 WAT 源码进行语法高亮
    /// </summary>
    public IReadOnlyList<HighlightSpan> Highlight(string source)
    {
        var lexer = new WatLexer();
        var tokens = lexer.Tokenize(source);
        var spans = new List<HighlightSpan>(tokens.Count);
        var offset = 0;

        foreach (var token in tokens)
        {
            var kind = token.Type switch
            {
                WatTokenType.Keyword => HighlightKind.keyword,
                WatTokenType.Opcode => HighlightKind.keyword,
                WatTokenType.ValueType => HighlightKind.type_name,
                WatTokenType.Number => HighlightKind.number,
                WatTokenType.StringLiteral => HighlightKind.@string,
                WatTokenType.Comment => HighlightKind.comment,
                WatTokenType.Identifier => HighlightKind.identifier,
                WatTokenType.Punctuation => HighlightKind.delimiter,
                _ => HighlightKind.other
            };
            var length = token.Value.Length;
            spans.Add(new HighlightSpan { kind = kind, offset = offset, length = length });
            offset += length;
        }

        return spans;
    }
}