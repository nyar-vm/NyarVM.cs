using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 方法定义。
/// </summary>
public sealed class HirMethodDef : HirCallable
{
    public HirMethodDef(
        string name,
        string memberName,
        DeclareObjectMethod syntax,
        IReadOnlyList<HirSymbolRef> parameters,
        HirTypeRef returnType,
        HirCallableSemantics semantics,
        IReadOnlyList<HirAttribute> surfaceAttributes,
        string? namespaceName,
        HirTypeRef ownerType,
        HirTypeRef? contractType,
        HirMethodKind kind,
        int? slotIndex,
        EffectSet effectSet)
        : base(name, parameters, returnType, semantics, surfaceAttributes, namespaceName, effectSet)
    {
        this.syntax = syntax;
        member_name = memberName;
        owner_type = ownerType;
        contract_type = contractType;
        this.kind = kind;
        slot_index = slotIndex;
    }

    public override AstNode syntax { get; }

    public override string member_name { get; }

    public override HirTypeRef owner_type { get; }

    public override HirTypeRef? contract_type { get; }

    public override HirMethodKind kind { get; }

    /// <summary>
    ///     Trait 方法槽位；仅对 trait 方法和其对应的 imply 实现有效。
    /// </summary>
    public override int? slot_index { get; }

    public override BlockStmt? body => ((DeclareObjectMethod)syntax).body;
}
