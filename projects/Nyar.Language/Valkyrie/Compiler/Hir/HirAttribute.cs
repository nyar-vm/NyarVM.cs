namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 函数属性。
/// </summary>
public sealed record HirAttribute(string name, IReadOnlyList<string> arguments);