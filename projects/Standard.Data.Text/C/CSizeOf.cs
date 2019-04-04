using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     sizeof 表达式

/// 。</summary>
public sealed record CSizeOf(CAstNode operand, TextSpan Span = default(TextSpan))
    : CAstNode(Span);