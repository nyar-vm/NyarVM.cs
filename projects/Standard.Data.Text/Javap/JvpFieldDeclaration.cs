using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     字段声明
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="type_name">类型名。</param>
/// <param name="field_name">字段名。</param>
/// <param name="span">源码位置。</param>
public sealed record JvpFieldDeclaration(
    List<string> access_flags,
    string type_name,
    string field_name,
    TextSpan span = default) : JvpAstNode(span);