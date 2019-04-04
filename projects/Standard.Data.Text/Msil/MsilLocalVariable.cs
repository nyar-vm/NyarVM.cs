using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     局部变量
/// </summary>
/// <param name="type_name">类型名。</param>
/// <param name="name">变量名。</param>
/// <param name="index">索引。</param>
/// <param name="span">源码位置。</param>
public sealed record MsilLocalVariable(string type_name, string? name = null, int index = 0, TextSpan span = default)
    : MsilAstNode(span);