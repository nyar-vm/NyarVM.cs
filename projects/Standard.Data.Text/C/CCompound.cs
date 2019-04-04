using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>
///     复合语句（代码块）
/// 。</summary>
public sealed record CCompound(IReadOnlyList<CAstNode> statements, TextSpan Span = default(TextSpan)) : CAstNode(Span);