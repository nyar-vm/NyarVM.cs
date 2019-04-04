using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Lexer;

/// <summary>
///     Valkyrie 语言的 NodeKind 分类器实现
/// </summary>
public sealed class ValkyrieNodeKindClassifier : INodeKindClassifier
{
    /// <summary>
    ///     单例
    /// </summary>
    public static readonly ValkyrieNodeKindClassifier instance = new();

    /// <inheritdoc />
    public bool is_keyword(NodeKind kind)
    {
        return kind.is_keyword();
    }

    /// <inheritdoc />
    public bool is_operator(NodeKind kind)
    {
        return kind.is_operator();
    }

    /// <inheritdoc />
    public bool is_literal(NodeKind kind)
    {
        return kind.is_literal();
    }

    /// <inheritdoc />
    public NodeKind statement_end_kind => ValkyrieTokenKind.semicolon.to_node_kind();

    /// <inheritdoc />
    public NodeKind block_open_kind => ValkyrieTokenKind.brace_l.to_node_kind();

    /// <inheritdoc />
    public NodeKind block_close_kind => ValkyrieTokenKind.brace_r.to_node_kind();

    /// <inheritdoc />
    public NodeKind end_of_stream_kind => ValkyrieTokenKind.eos.to_node_kind();
}