using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 关联类型绑定，如 <c>type Resume = i32</c>。
///     用于在 <c>imply</c> 块中绑定 trait 的关联类型到具体类型。
/// </summary>
public sealed record HirTypeBinding(
    /// <summary>
    ///     trait 名称
    /// </summary>
    SemanticNamePath? trait_name,
    /// <summary>
    ///     关联类型名称
    /// </summary>
    string associated_type_name,
    /// <summary>
    ///     绑定的具体类型
    /// </summary>
    HirTypeRef concrete_type
);
