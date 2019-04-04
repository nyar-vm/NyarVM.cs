using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Ref;

/// <summary>
///     HIR trait 别名引用。
/// </summary>
public sealed record HirTraitAliasRef(
    string name,
    ValkyrieNamePath name_path,
    IReadOnlyList<HirTypeRef> target_traits)
{
    /// <summary>
    ///     别名简单名称路径。
    /// </summary>
    public ValkyrieNamePath simple_name_path { get; } = ValkyrieNamePath.parse(name);

    /// <summary>
    ///     别名完整名称文本，仅供打印和调试。
    /// </summary>
    public string full_name => name_path.ToString();
}
