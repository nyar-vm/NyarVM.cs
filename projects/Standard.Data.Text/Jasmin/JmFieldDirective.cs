using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     .field 指令
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="field_name">字段名。</param>
/// <param name="descriptor">字段描述符。</param>
/// <param name="initial_value">初始值。</param>
/// <param name="span">源码位置。</param>
public sealed record JmFieldDirective(
    List<string> access_flags,
    string field_name,
    string descriptor,
    string? initial_value = null,
    TextSpan span = default) : JmAstNode(span);