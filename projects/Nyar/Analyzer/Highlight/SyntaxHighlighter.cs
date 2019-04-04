using TextSpan = Std.Text.TextSpan;

namespace Nyar.Analyzer.Highlight;

public class SyntaxHighlighter
{
    private readonly NodeKindClassifier? _node_classifier;
    private readonly Dictionary<int, HighlightKind> _node_kind_map;
    private readonly TokenKindClassifier? _token_classifier;
    private readonly Dictionary<int, HighlightKind> _token_kind_map;

    public SyntaxHighlighter()
    {
        _token_classifier = null;
        _node_classifier = null;
        _token_kind_map = new Dictionary<int, HighlightKind>();
        _node_kind_map = new Dictionary<int, HighlightKind>();
    }

    public SyntaxHighlighter(TokenKindClassifier tokenClassifier, NodeKindClassifier nodeClassifier)
    {
        _token_classifier = tokenClassifier;
        _node_classifier = nodeClassifier;
        _token_kind_map = new Dictionary<int, HighlightKind>();
        _node_kind_map = new Dictionary<int, HighlightKind>();
    }

    public void map_token_kind(int tokenType, HighlightKind highlightKind)
    {
        _token_kind_map[tokenType] = highlightKind;
    }

    public void map_node_kind(int nodeKind, HighlightKind highlightKind)
    {
        _node_kind_map[nodeKind] = highlightKind;
    }

    public HighlightKind classify_token(int tokenType)
    {
        if (_token_kind_map.TryGetValue(tokenType, out var kind)) return kind;

        if (_token_classifier is not null) return _token_classifier(tokenType);

        return HighlightKind.none;
    }

    public HighlightKind classify_node(int nodeKind)
    {
        if (_node_kind_map.TryGetValue(nodeKind, out var kind)) return kind;

        if (_node_classifier is not null) return _node_classifier(nodeKind);

        return HighlightKind.none;
    }

    public IReadOnlyList<HighlightToken> highlight_tokens(IReadOnlyList<(int Kind, TextSpan Span)> tokens)
    {
        var results = new List<HighlightToken>(tokens.Count);

        foreach (var (kind, span) in tokens)
        {
            var highlightKind = classify_token(kind);
            if (highlightKind != HighlightKind.none) results.Add(new HighlightToken(highlightKind, span));
        }

        return results;
    }

    public IReadOnlyList<HighlightToken> highlight_nodes(IReadOnlyList<(int Kind, TextSpan Span)> nodes)
    {
        var results = new List<HighlightToken>(nodes.Count);

        foreach (var (kind, span) in nodes)
        {
            var highlightKind = classify_node(kind);
            if (highlightKind != HighlightKind.none) results.Add(new HighlightToken(highlightKind, span));
        }

        return results;
    }

    /// TODO: 待实现 - ILanguage 类型暂不可用
    // public static SyntaxHighlighter CreateForLanguage(global::Nyar.Language.ILanguage language)
    // {
    //     return new SyntaxHighlighter();
    // }
}