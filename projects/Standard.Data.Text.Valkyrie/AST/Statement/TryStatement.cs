using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Try 语句，将一组效应操作转为 <c>Result&lt;T, E&gt;</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// try Result&lt;String, [Log]&gt; {
///     business()
///         .catch { case Get: resume(state) }
/// }
/// .match {
///     case Fine(s): s
///     case Fail(Log { msg }): print("error: " + msg)
/// }
/// </code>
/// <remarks>
///     语义：
///     <list type="bullet">
///         <item>正常返回 <c>v : T</c> 则整体为 <c>Fine(v)</c></item>
///         <item>若内部 <c>raise</c> 了属于捕获列表的效应操作，则计算立即终止，整体为 <c>Fail(op)</c></item>
///     </list>
/// </remarks>
public sealed record TryStatement : ValkyrieNode
{
    /// <summary>
    ///     完整的 Result 类型表达式，如 <c>Result&lt;String, [Log]&gt;</c>
    /// </summary>
    public TypeNode result_type { get; init; } = new TypeLiteralNamePathNode();

    /// <summary>
    ///     被捕获的效应类型列表
    /// </summary>
    public IReadOnlyList<TypeNode> capture_types { get; init; } = [];

    /// <summary>
    ///     受保护的代码块
    /// </summary>
    public FunctionBody body { get; init; } = new();
}