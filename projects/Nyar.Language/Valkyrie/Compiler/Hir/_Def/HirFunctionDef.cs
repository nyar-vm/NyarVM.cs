using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir._Def;

/// <summary>
///     HIR 自由函数定义。
/// </summary>
public sealed class HirFunctionDef : HirCallable
{
    public HirFunctionDef(
        string name,
        FunctionDecl syntax,
        IReadOnlyList<HirSymbolRef> parameters,
        HirTypeRef returnType,
        HirCallableSemantics semantics,
        IReadOnlyList<HirAttribute> surfaceAttributes,
        string? namespaceName,
        EffectSet effectSet)
        : base(name, parameters, returnType, semantics, surfaceAttributes, namespaceName, effectSet)
    {
        this.syntax = syntax;
    }

    public override AstNode syntax { get; }

    public override string member_name => ((FunctionDecl)syntax).name?.name ?? string.Empty;

    public override HirTypeRef owner_type => HirTypeRef.named(ValkyrieNamePath.parse(null));

    public override HirTypeRef? contract_type => null;

    public override HirMethodKind kind => HirMethodKind.inherent;

    public override int? slot_index => null;

    public override BlockStmt? body => ((FunctionDecl)syntax).body;
}
