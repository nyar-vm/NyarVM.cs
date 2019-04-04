using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR 继承边定义。
/// </summary>
public sealed record HirInheritanceEdge(
    string field_name,
    HirTypeRef base_type,
    HirTypeRef storage_type,
    HirInheritanceStorageKind storage_kind);