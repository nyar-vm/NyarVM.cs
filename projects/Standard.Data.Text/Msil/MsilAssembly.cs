using Std.Data.Text.Syntax;

namespace Std.Data.Text.Msil;

/// <summary>
///     ILASM 程序集
/// </summary>
/// <param name="name">程序集名。</param>
/// <param name="version">版本号。</param>
/// <param name="namespaces">命名空间列表。</param>
/// <param name="span">源码位置。</param>
public sealed record MsilAssembly(string name, string? version, List<MsilNamespace> namespaces, TextSpan span = default)
    : MsilAstNode(span);