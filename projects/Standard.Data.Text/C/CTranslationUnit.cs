using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     翻译单元（根节点）

/// 。</summary>
public sealed record CTranslationUnit(IReadOnlyList<CAstNode> declarations, TextSpan Span = default(TextSpan))
    : CAstNode(Span);