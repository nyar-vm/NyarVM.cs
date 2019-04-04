using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.Types.Externals;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 类型定义基底。
/// </summary>
public abstract record HirTypeDef
{
    /// <summary>
    ///     类型的完整名称路径。
    /// </summary>
    public ValkyrieNamePath namepath { get; }

    /// <summary>
    ///     当前定义是否具有 `[data]` 结构形状。
    /// </summary>
    public bool is_data_type => data_shape is not null;

    public virtual HirDataShape? data_shape { get; init; }
    public virtual IReadOnlyList<HirMethod> methods { get; init; } = [];
    public virtual IReadOnlyList<ExternalImport> external_import_links { get; init; } = [];
    public virtual IReadOnlyList<HirTypeRef> inherited_types { get; init; } = [];
    public string name => namepath.name;
    public HirTypeKind kind { get; }

    /// <summary>
    ///     HIR 类型定义基底。
    /// </summary>
    protected HirTypeDef(ValkyrieNamePath namepath, HirTypeKind kind)
    {
        this.namepath = namepath;
        this.kind = kind;
    }

    /// <summary>
    ///     判断当前类型定义是否匹配查询名称。
    /// </summary>
    public bool matches_name(ValkyrieNamePath query, ValkyrieNameSpace? currentNamespace = null)
    {
        return HirTypeRef.matches_name_path(new([name]), namepath, query, currentNamespace);
    }

    /// <summary>
    ///     使用外部解析器递归收集当前类型的继承层级。
    /// </summary>
    public IReadOnlyList<HirTypeDef> resolve_hierarchy(Func<ValkyrieNamePath, HirTypeDef?> resolveBaseType)
    {
        var results = new List<HirTypeDef>();
        collect_hierarchy(resolveBaseType, results, new HashSet<ValkyrieNamePath>());
        return results;
    }

    /// <summary>
    ///     将当前类型及其基类型递归写入结果集合。
    /// </summary>
    private void collect_hierarchy(
        Func<ValkyrieNamePath, HirTypeDef?> resolveBaseType,
        ICollection<HirTypeDef> results,
        ISet<ValkyrieNamePath> visited
    )
    {
        if (!visited.Add(namepath)) return;

        results.Add(this);
        foreach (var inheritedType in inherited_types)
        {
            var resolvedBase = resolveBaseType(inheritedType.name_path);
            resolvedBase?.collect_hierarchy(resolveBaseType, results, visited);
        }
    }
}
