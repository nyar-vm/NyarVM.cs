using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR Trait 定义。
/// </summary>
public sealed record HirTraitDef
{
    /// <summary>
    ///     Trait 的完整名称路径。
    /// </summary>
    public ValkyrieNamePath name_path { get; init; }

    public string name => name_path.name;
    public IReadOnlyList<HirMethod> methods { get; init; }
    public IReadOnlyList<HirTypeRef> base_traits { get; init; }
    public IReadOnlyList<string> associated_type_names { get; init; }

    /// <summary>
    ///     HIR Trait 定义。
    /// </summary>
    public HirTraitDef(ValkyrieNamePath name_path,
        IReadOnlyList<HirMethod> methods,
        IReadOnlyList<HirTypeRef> base_traits,
        IReadOnlyList<string> associated_type_names)
    {
        this.name_path = name_path;
        this.methods = methods;
        this.base_traits = base_traits;
        this.associated_type_names = associated_type_names;
    }
    /// <summary>
    ///     判断当前 trait 是否匹配查询名称。
    /// </summary>
    public bool matches_name(ValkyrieNamePath query, ValkyrieNameSpace? currentNamespace = null)
    {
        return HirTypeRef.matches_name_path(new([name]), name_path, query, currentNamespace);
    }

    /// <summary>
    ///     解析当前 trait 的完整合同方法集合，包含继承而来的方法。
    /// </summary>
    public IReadOnlyList<HirMethod> enumerate_contract_methods(
        Func<ValkyrieNamePath, IReadOnlyList<HirTraitDef>> resolveBaseTraits)
    {
        var resolvedMethods = new Dictionary<string, HirMethod>(StringComparer.Ordinal);

        foreach (var baseTrait in base_traits)
        foreach (var resolvedBaseTrait in resolveBaseTraits(baseTrait.name_path))
        foreach (var inheritedMethod in resolvedBaseTrait.enumerate_contract_methods(resolveBaseTraits))
            resolvedMethods.TryAdd(inheritedMethod.member_name, inheritedMethod);

        foreach (var method in methods) resolvedMethods[method.member_name] = method;

        return [.. resolvedMethods.Values];
    }

    /// <summary>
    ///     将当前 trait 及其基 trait 递归写入结果集合。
    /// </summary>
    public void collect_hierarchy(
        Func<ValkyrieNamePath, IReadOnlyList<HirTraitDef>> resolveBaseTraits,
        ICollection<HirTraitDef> results,
        ISet<ValkyrieNamePath> visited)
    {
        if (!visited.Add(name_path)) return;

        results.Add(this);
        foreach (var baseTrait in base_traits)
        foreach (var resolvedBaseTrait in resolveBaseTraits(baseTrait.name_path))
            resolvedBaseTrait.collect_hierarchy(resolveBaseTraits, results, visited);
    }
}
