namespace Nyar.Language.Valkyrie.Compiler.Hir._Ref;

/// <summary>
///     HIR 符号引用。
/// </summary>
public sealed record HirSymbolRef(string name, HirTypeRef type)
{
}
