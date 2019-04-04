using Std.Data.Text.Syntax;

namespace Std.Data.Text.Parsing;

/// <summary>
///     NodeKind 分类器，由各语言提供，用于 TokenStream 判断 Token 类型
/// </summary>
public interface INodeKindClassifier
{
    /// <summary>
    ///     获取语句结束符的 Kind（如分号），用于错误恢复时的 Synchronize
    /// </summary>
    NodeKind statement_end_kind { get; }

    /// <summary>
    ///     获取左大括号的 Kind，用于错误恢复时的 Synchronize
    /// </summary>
    NodeKind block_open_kind { get; }

    /// <summary>
    ///     获取右大括号的 Kind，用于错误恢复时的 Synchronize
    /// </summary>
    NodeKind block_close_kind { get; }

    /// <summary>
    ///     获取流结束符的 Kind（如 Eos/Eof），用于判断是否到达 Token 流末尾
    /// </summary>
    NodeKind end_of_stream_kind { get; }

    /// <summary>
    ///     判断指定 Kind 是否为关键词
    /// </summary>
    bool is_keyword(NodeKind kind);

    /// <summary>
    ///     判断指定 Kind 是否为操作符
    /// </summary>
    bool is_operator(NodeKind kind);

    /// <summary>
    ///     判断指定 Kind 是否为字面量
    /// </summary>
    bool is_literal(NodeKind kind);
}