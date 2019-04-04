using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     结构体定义

/// 。</summary>
public sealed record CStructDef(
    string? name,
    IReadOnlyList<CVarDecl> fields,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);