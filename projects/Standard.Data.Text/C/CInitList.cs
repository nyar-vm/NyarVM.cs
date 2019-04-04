using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     数组初始化列表

/// 。</summary>
public sealed record CInitList(IReadOnlyList<CAstNode> elements, TextSpan Span = default(TextSpan))
    : CAstNode(Span);