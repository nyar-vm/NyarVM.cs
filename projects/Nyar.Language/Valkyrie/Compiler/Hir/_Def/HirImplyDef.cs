using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR `imply` 定义。
/// </summary>
public sealed record HirImplyDef
{
    /// <summary>
    ///     所属命名空间的结构化表示。
    /// </summary>
    public ValkyrieNameSpace namespace_path => ValkyrieNameSpace.parse(namespace_name);

    public HirTypeRef target_type { get; init; }
    public HirTypeRef? contract_type { get; init; }
    public string? namespace_name { get; init; }
    public IReadOnlyList<HirMethod> methods { get; init; }
    public IReadOnlyList<HirWitnessBinding> witness_bindings { get; init; }
    public IReadOnlyList<HirTypeBinding> type_bindings { get; init; }
    public HirImplySourceKind source_kind { get; init; }
    /// <summary>
    ///     HIR `imply` 定义。
    /// </summary>
    public HirImplyDef(HirTypeRef target_type,
        HirTypeRef? contract_type,
        string? namespace_name,
        IReadOnlyList<HirMethod> methods,
        IReadOnlyList<HirWitnessBinding> witness_bindings,
        IReadOnlyList<HirTypeBinding> type_bindings,
        HirImplySourceKind source_kind)
    {
        this.target_type = target_type;
        this.contract_type = contract_type;
        this.namespace_name = namespace_name;
        this.methods = methods;
        this.witness_bindings = witness_bindings;
        this.type_bindings = type_bindings;
        this.source_kind = source_kind;
    }
    /// <summary>
    ///     判断当前 `imply` 的目标类型是否匹配查询类型。
    /// </summary>
    public bool matches_target_type(ValkyrieNamePath query, ValkyrieNameSpace? currentNamespace = null)
    {
        return HirTypeRef.matches_name_path(
            target_type.name_path,
            namespace_path.qualify(ValkyrieNamePath.parse(target_type.name)),
            query,
            currentNamespace);
    }

    /// <summary>
    ///     判断当前 `imply` 的合同类型是否匹配查询类型。
    /// </summary>
    public bool matches_contract_type(ValkyrieNamePath query, ValkyrieNameSpace? currentNamespace = null)
    {
        if (contract_type is null) return false;

        return HirTypeRef.matches_name_path(
            contract_type.name_path,
            namespace_path.qualify(ValkyrieNamePath.parse(contract_type.name)),
            query,
            currentNamespace);
    }
}
