using Nyar.Language.Valkyrie.Compiler.Hir._Ref;
using Std.Data.Text.Valkyrie.AST.Term;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR Raise 节点，表示效应抛出操作。
/// </summary>
public sealed record HirRaise
{
    /// <summary>
    ///     被抛出的效应操作表达式。
    /// </summary>
    public TermNode value { get; init; } = new TermLiteralNamePathNode();

    /// <summary>
    ///     期望恢复类型，<c>None</c> 表示不可恢复效应。
    /// </summary>
    public HirTypeRef? resume_type { get; init; }
}