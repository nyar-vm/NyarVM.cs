using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     内存声明
/// </summary>
/// <param name="InitialPages">初始页数。</param>
/// <param name="MaxPages">最大页数。</param>
/// <param name="Name">内存名。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatMemory(uint InitialPages, uint? MaxPages, string? Name = null, TextSpan Span = default)
    : WatAstNode(Span);