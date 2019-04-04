using Std.Data.Text.Syntax;

namespace Std.Data.Text.Jasmin;

/// <summary>
///     Jasmin 类文件
/// </summary>
/// <param name="class_directive">.class 指令</param>
/// <param name="super_directive">.super 指令</param>
/// <param name="implements_directives">.implements 指令列表</param>
/// <param name="fields">字段列表。</param>
/// <param name="methods">方法列表。</param>
/// <param name="span">源码位置。</param>
public sealed record JmClassFile(
    JmClassDirective class_directive,
    JmSuperDirective super_directive,
    List<JmImplementsDirective> implements_directives,
    List<JmFieldDirective> fields,
    List<JmMethodDeclaration> methods,
    TextSpan span = default) : JmAstNode(span);