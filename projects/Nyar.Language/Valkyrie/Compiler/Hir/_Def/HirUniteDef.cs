using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 值语义 `unite` 定义。
/// </summary>
public sealed record HirUniteDef : HirTypeDef
{
    public override IReadOnlyList<HirMethod> methods { get; init; }
    public IReadOnlyList<HirVariantDef> variants { get; init; }

    /// <summary>
    ///     HIR 值语义 `unite` 定义。
    /// </summary>
    public HirUniteDef(ValkyrieNamePath namepath,
        IReadOnlyList<HirMethod> methods,
        IReadOnlyList<HirVariantDef> variants) : base(namepath,
        HirTypeKind.unite)
    {
        this.methods = methods;
        this.variants = variants;
    }
}
