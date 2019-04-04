namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 特殊类型种类。
/// </summary>
public enum HirSpecialTypeKind
{
    none,
    unit,
    @void,
    auto,
    exit_code
}