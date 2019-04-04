using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Types.Externals;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 类定义。
/// </summary>
public sealed record HirClassDef : HirTypeDef
{
    public override HirDataShape? data_shape { get; init; }
    public override IReadOnlyList<HirMethod> methods { get; init; }
    public IReadOnlyList<HirAttribute> surface_attributes { get; init; }
    public override IReadOnlyList<ExternalImport> external_import_links { get; init; }
    public HirTypeRef? base_type { get; init; }
    public override IReadOnlyList<HirTypeRef> inherited_types { get; init; }
    public IReadOnlyList<HirInheritanceEdge> inheritance_edges { get; init; }

    /// <summary>
    ///     HIR 类定义。
    /// </summary>
    public HirClassDef(
        ValkyrieNamePath namepath,
        HirDataShape? data_shape,
        IReadOnlyList<HirMethod> methods,
        IReadOnlyList<HirAttribute> surface_attributes,
        IReadOnlyList<ExternalImport> external_import_links,
        HirTypeRef? base_type,
        IReadOnlyList<HirTypeRef> inherited_types,
        IReadOnlyList<HirInheritanceEdge> inheritance_edges) : base(namepath, HirTypeKind.@class)
    {
        this.data_shape = data_shape;
        this.methods = methods;
        this.surface_attributes = surface_attributes;
        this.external_import_links = external_import_links;
        this.base_type = base_type;
        this.inherited_types = inherited_types;
        this.inheritance_edges = inheritance_edges;
    }
}
