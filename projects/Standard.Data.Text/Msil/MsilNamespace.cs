using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     命名空间
/// </summary>
/// <param name="name">命名空间名。</param>
/// <param name="types">类型列表。</param>
/// <param name="span">源码位置。</param>
public sealed record MsilNamespace(string name, List<MsilClassDeclaration> types, TextSpan span = default)
    : MsilAstNode(span);