using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     .limit 指令
/// </summary>
/// <param name="limit_kind">限制类型（stack / locals）。</param>
/// <param name="value">限制值。</param>
/// <param name="span">源码位置。</param>
public sealed record JmLimitDirective(string limit_kind, int value, TextSpan span = default) : JmAstNode(span);