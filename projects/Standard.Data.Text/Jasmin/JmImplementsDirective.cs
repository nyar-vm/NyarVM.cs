using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     .implements 指令
/// </summary>
/// <param name="interface_name">接口名（内部格式）。</param>
/// <param name="span">源码位置。</param>
public sealed record JmImplementsDirective(string interface_name, TextSpan span = default) : JmAstNode(span);