using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     类声明
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="class_kind">类种类（class/interface/enum）。</param>
/// <param name="class_name">类名（全限定名）。</param>
/// <param name="extends">父类。</param>
/// <param name="implements">实现的接口。</param>
/// <param name="fields">字段列表。</param>
/// <param name="methods">方法列表。</param>
/// <param name="span">源码位置。</param>
public sealed record JvpClassDeclaration(
    List<string> access_flags,
    string class_kind,
    string class_name,
    string? extends,
    List<string> implements,
    List<JvpFieldDeclaration> fields,
    List<JvpMethodDeclaration> methods,
    TextSpan span = default) : JvpAstNode(span);