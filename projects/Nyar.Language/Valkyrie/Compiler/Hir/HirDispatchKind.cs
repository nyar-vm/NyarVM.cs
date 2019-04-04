namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 调用分派种类。
/// </summary>
public enum HirDispatchKind
{
    @static,
    dynamic,
    witness
}