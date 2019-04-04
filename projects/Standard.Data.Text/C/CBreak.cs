using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     Break 语句


/// </summary>
public sealed record CBreak(TextSpan Span = default(TextSpan))
    : CAstNode(Span);