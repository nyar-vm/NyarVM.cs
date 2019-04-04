using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     方法声明
/// </summary>
/// <param name="access_flags">访问标志。</param>
/// <param name="return_type_name">返回类型名。</param>
/// <param name="method_name">方法名。</param>
/// <param name="parameters">参数列表。</param>
/// <param name="code_section">Code 段。</param>
/// <param name="span">源码位置。</param>
public sealed record JvpMethodDeclaration(
    List<string> access_flags,
    string return_type_name,
    string method_name,
    List<JvpParameter> parameters,
    JvpCodeSection? code_section,
    TextSpan span = default) : JvpAstNode(span);