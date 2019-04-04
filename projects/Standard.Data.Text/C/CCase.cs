using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     Case 分支

/// 。</summary>
public sealed record CCase(CAstNode? value, IReadOnlyList<CAstNode> body, TextSpan Span = default(TextSpan))
    : CAstNode(Span);