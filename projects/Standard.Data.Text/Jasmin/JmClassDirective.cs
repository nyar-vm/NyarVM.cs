using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     .class 指令
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="class_name">类名（内部格式）。</param>
/// <param name="span">源码位置。</param>
public sealed record JmClassDirective(List<string> access_flags, string class_name, TextSpan span = default)
    : JmAstNode(span);