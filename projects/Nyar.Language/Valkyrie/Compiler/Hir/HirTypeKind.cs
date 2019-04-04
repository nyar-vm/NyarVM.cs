namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 类型种类。
/// </summary>
public enum HirTypeKind
{
    @class,
    structure,
    enums,
    flags,
    widget,
    shader,
    component,
    system,
    micro,
    macro,
    mezzo,
    neural,
    schema,
    unite,
    trait
}