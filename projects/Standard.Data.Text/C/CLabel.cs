using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     标签语句

/// 。</summary>
public sealed record CLabel(string name, CAstNode statement, TextSpan Span = default(TextSpan))
    : CAstNode(Span);