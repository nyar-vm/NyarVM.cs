using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     Switch 语句

/// 。</summary>
public sealed record CSwitch(
    CAstNode expression,
    IReadOnlyList<CAstNode> cases,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);