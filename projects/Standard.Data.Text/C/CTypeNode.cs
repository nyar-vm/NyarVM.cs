using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     类型节点

/// 。</summary>
public sealed record CTypeNode(string name, bool is_pointer, bool is_const, TextSpan Span = default(TextSpan))
    : CAstNode(Span);