using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 命名类型实参绑定，例如 <c>Item = i32</c>。
/// </summary>
public sealed record HirTypeArgumentBinding(
    string slot_name,
    HirTypeRef type);