using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Semantic;

/// <summary>
///     全局声明表，聚合所有文件的模块、类型、trait、函数和 witness table 信息
/// </summary>
public sealed class GlobalDeclarationTable
{
    /// <summary>
    ///     模块名 → 模块中的声明 AST 列表
    /// </summary>
    public Dictionary<string, List<object>> modules { get; } = new();

    /// <summary>
    ///     完全限定类型名 → 类型定义
    /// </summary>
    public Dictionary<SemanticNamePath, HirTypeDef> types { get; } = new();

    /// <summary>
    ///     完全限定 trait 名 → trait 定义
    /// </summary>
    public Dictionary<SemanticNamePath, HirTraitDef> traits { get; } = new();

    /// <summary>
    ///     完全限定函数名 → 函数签名
    /// </summary>
    public Dictionary<SemanticNamePath, HirFunction> functions { get; } = new();

    /// <summary>
    ///     类型 → 满足的 trait 集合
    /// </summary>
    public Dictionary<SemanticNamePath, HashSet<SemanticNamePath>> type_trait_satisfactions { get; } = new();

    /// <summary>
    ///     (TypeName, TraitName) → 编译期证据表
    /// </summary>
    public Dictionary<(SemanticNamePath TypeName, SemanticNamePath TraitName), object> witness_tables { get; } = new();

    /// <summary>
    ///     按限定名查找类型
    /// </summary>
    public bool try_get_type(SemanticNamePath qualifiedName, out HirTypeDef typeDef)
    {
        return types.TryGetValue(qualifiedName, out typeDef);
    }

    /// <summary>
    ///     按限定名查找 trait
    /// </summary>
    public bool try_get_trait(SemanticNamePath qualifiedName, out HirTraitDef traitDef)
    {
        return traits.TryGetValue(qualifiedName, out traitDef);
    }

    /// <summary>
    ///     按限定名查找函数
    /// </summary>
    public bool try_get_function(SemanticNamePath qualifiedName, out HirFunction method)
    {
        return functions.TryGetValue(qualifiedName, out method);
    }
}
