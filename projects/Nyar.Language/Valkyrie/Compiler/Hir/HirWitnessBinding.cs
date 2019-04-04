using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     `imply` 方法对 trait 槽位的绑定关系。
/// </summary>
public sealed record HirWitnessBinding(
    SemanticNamePath trait_name,
    int slot_index,
    string method_name,
    HirMethod implementation);
