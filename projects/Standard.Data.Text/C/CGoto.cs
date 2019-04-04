using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     Goto 语句

/// 。</summary>
public sealed record CGoto(string label, TextSpan Span = default(TextSpan))
    : CAstNode(Span);