using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     `loop item in items { }` 的结构化迭代协议解析结果。
/// </summary>
public sealed record HirLoopIterationResolution(
    HirCallResolution? iterator_factory,
    HirCallResolution has_next_call,
    HirCallResolution next_call,
    HirCallResolution? unwrap_call,
    HirTypeRef item_type);