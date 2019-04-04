using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Compiler.Hir.Attributes;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 顶层 `let` 定义。
/// </summary>
public sealed class HirLetDefs
{
    /// <summary>
    ///     HIR 顶层 `let` 定义。
    /// </summary>
    public HirLetDefs(
        ValkyrieNamePath namepath,
        LetDeclaration syntax,
        HirTypeRef type,
        IReadOnlyList<HirAttribute> surfaceAttributes)
    {
        this.namepath = namepath;
        this.syntax = syntax;
        this.type = type;
        surface_attributes = surfaceAttributes;
    }

    /// <summary>
    ///     局部或顶层绑定的完整名称路径。
    /// </summary>
    public ValkyrieNamePath namepath { get; }

    /// <summary>
    ///     对应的真实 `let` 语句节点。
    /// </summary>
    public LetDeclaration syntax { get; }

    /// <summary>
    ///     绑定名称。
    /// </summary>
    public string name => syntax.name?.name ?? namepath.name;

    /// <summary>
    ///     绑定类型；优先使用显式标注，否则做保守推断。
    /// </summary>
    public HirTypeRef type { get; }

    /// <summary>
    ///     是否为可变绑定。
    /// </summary>
    public bool is_mutable => syntax.is_mutable;

    /// <summary>
    ///     绑定初始化表达式。
    /// </summary>
    public AstNode? initializer => syntax.initializer;

    /// <summary>
    ///     未被元语言消费的表面属性。
    /// </summary>
    public IReadOnlyList<HirAttribute> surface_attributes { get; }

    /// <summary>
    ///     判断当前 `let` 是否匹配查询名称。
    /// </summary>
    public bool matches_name(ValkyrieNamePath query, ValkyrieNameSpace? currentNamespace = null)
    {
        return HirTypeRef.matches_name_path(new([name]), namepath, query, currentNamespace);
    }
}
