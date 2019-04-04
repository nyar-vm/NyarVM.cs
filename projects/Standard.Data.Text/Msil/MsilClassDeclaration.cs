using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     类声明
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="type_kind">类型种类（class / value type）。</param>
/// <param name="name">类型名。</param>
/// <param name="methods">方法列表。</param>
/// <param name="fields">字段列表。</param>
/// <param name="span">源码位置。</param>
public sealed record MsilClassDeclaration(
    List<string> access_flags,
    string type_kind,
    string name,
    List<MsilMethodDeclaration> methods,
    List<MsilFieldDeclaration> fields,
    TextSpan span = default) : MsilAstNode(span);