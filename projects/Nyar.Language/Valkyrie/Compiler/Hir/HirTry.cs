using Nyar.Language.Valkyrie.Compiler.Hir._Ref;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     HIR Try 节点，表示 <c>try Result&lt;T, [E]&gt; { ... }</c> 效应转 Result 的受保护块。
/// </summary>
public sealed record HirTry
{
    /// <summary>
    ///     完整的 Result 类型表达式，如 <c>Result&lt;String, [Log]&gt;</c>。
    /// </summary>
    public HirTypeRef result_type { get; init; } = HirTypeRef.unknown();

    /// <summary>
    ///     被捕获的效应类型列表。
    /// </summary>
    public IReadOnlyList<HirTypeRef> capture_effects { get; init; } = [];

    /// <summary>
    ///     受保护的代码块。
    /// </summary>
    public BlockStmt body { get; init; } = new();
}