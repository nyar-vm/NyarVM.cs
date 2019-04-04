using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 调用选择结果。
/// </summary>
public sealed record HirCallResolution(
    HirDispatchKind dispatch,
    string target_name,
    bool inject_receiver,
    int? method_index,
    SemanticNamePath? witness_trait_name,
    string? member_name)
;
