using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.Parser;

internal static class ParserNodeFactory
{
    internal static IdentifierNode create_identifier(string text)
    {
        if (text is ['`', _, ..] && text[^1] == '`') return new IdentifierNode(text[1..^1], true);

        return new IdentifierNode(text);
    }

    internal static QualifiedPathNode create_qualified_path(string text, bool isGlobal = false)
    {
        var segments = text
            .Split(["::", "."], StringSplitOptions.RemoveEmptyEntries)
            .Select(create_identifier)
            .ToList();

        return new QualifiedPathNode
        {
            is_global = isGlobal,
            segments = segments
        };
    }

    internal static QualifiedPathNode create_qualified_path(IReadOnlyList<IdentifierNode> segments,
        bool isGlobal = false)
    {
        return new QualifiedPathNode
        {
            is_global = isGlobal,
            segments = segments
        };
    }

    internal static TermLiteralNamePathNode create_term_literal_symbol(string text, bool isGlobal = false)
    {
        return new TermLiteralNamePathNode
        {
            path = create_qualified_path(text, isGlobal)
        };
    }

    internal static TermLiteralNamePathNode create_term_literal_symbol(IReadOnlyList<IdentifierNode> segments,
        bool isGlobal = false)
    {
        return new TermLiteralNamePathNode
        {
            path = create_qualified_path(segments, isGlobal)
        };
    }

    internal static TermLiteralNumberNode create_term_literal_number(string value)
    {
        return new TermLiteralNumberNode
        {
            value = value
        };
    }

    internal static TermLiteralTextNode create_term_literal_text(string value, TextLiteralKind literalKind,
        string prefix = "")
    {
        return new TermLiteralTextNode
        {
            prefix = prefix,
            value = value,
            literal_kind = literalKind
        };
    }

    internal static TermLiteralBooleanNode create_term_literal_boolean(bool value)
    {
        return new TermLiteralBooleanNode
        {
            value = value
        };
    }

    internal static LiteralNullNode create_term_literal_null()
    {
        return new LiteralNullNode();
    }

    internal static TypeLiteralNamePathNode create_type_literal_symbol(string text, bool isGlobal = false)
    {
        return new TypeLiteralNamePathNode
        {
            path = create_qualified_path(text, isGlobal)
        };
    }

    internal static TypeLiteralNamePathNode create_type_literal_symbol(IReadOnlyList<IdentifierNode> segments,
        bool isGlobal = false)
    {
        return new TypeLiteralNamePathNode
        {
            path = create_qualified_path(segments, isGlobal)
        };
    }

    internal static TermNode ensure_term_node(ValkyrieNode node)
    {
        return node switch
        {
            TermNode termNode => termNode,
            IdentifierNode identifier => create_term_literal_symbol([identifier]),
            _ => create_term_literal_symbol("<invalid-expression>")
        };
    }
}
