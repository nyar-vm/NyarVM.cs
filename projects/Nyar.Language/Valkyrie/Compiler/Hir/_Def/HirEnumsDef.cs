using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 枚举定义。
/// </summary>
public sealed record HirEnumsDef : HirTypeDef
{
    public IReadOnlyList<HirVariantDef> variants { get; init; }
    public int? enum_base_type { get; init; }

    /// <summary>
    ///     HIR 枚举定义。
    /// </summary>
    public HirEnumsDef(ValkyrieNamePath namepath,
        IReadOnlyList<HirVariantDef> variants,
        int? enum_base_type = null) : base(namepath,
        HirTypeKind.enums
    )
    {
        this.variants = variants;
        this.enum_base_type = enum_base_type;
    }
}
