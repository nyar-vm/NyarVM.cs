using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     .super 指令
/// </summary>
/// <param name="super_class_name">父类名（内部格式）。</param>
/// <param name="span">源码位置。</param>
public sealed record JmSuperDirective(string super_class_name, TextSpan span = default) : JmAstNode(span);