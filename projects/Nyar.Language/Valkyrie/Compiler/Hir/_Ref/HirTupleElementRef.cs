using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 元组元素类型引用。
/// </summary>
public sealed record HirTupleElementRef(
    string? label,
    HirTypeRef type);
