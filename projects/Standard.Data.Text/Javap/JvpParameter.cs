using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     方法参数
/// </summary>
/// <param name="type_name">类型名。</param>
/// <param name="name">参数名。</param>
/// <param name="span">源码位置。</param>
public sealed record JvpParameter(string type_name, string? name = null, TextSpan span = default) : JvpAstNode(span);