using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     表声明
/// </summary>
/// <param name="ElementType">元素类型。</param>
/// <param name="InitialSize">初始大小。</param>
/// <param name="MaxSize">最大大小。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatTable(string ElementType, uint InitialSize, uint? MaxSize = null, TextSpan Span = default)
    : WatAstNode(Span);