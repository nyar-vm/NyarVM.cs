using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Yield 关键字类型
/// </summary>
public enum YieldKeyword
{
    /// <summary>
    ///     <c>yield expr</c>，抛出生成器产出值
    /// </summary>
    Yield,

    /// <summary>
    ///     <c>yield break</c>，终止生成器
    /// </summary>
    YieldBreak,

    /// <summary>
    ///     <c>yield return expr</c>，产出值并终止生成器
    /// </summary>
    YieldReturn
}

/// <summary>
///     Yield 语句，用于生成器产出值或终止生成器
/// </summary>
/// <para>示例：</para>
/// <code>
/// yield 42;
/// yield break;
/// yield return 99;
/// </code>
/// <remarks>
///     语法糖脱糖规则：
///     <list type="bullet">
///         <item><c>yield expr</c> → <c>raise Yielder::Yield { value: expr }</c></item>
///         <item><c>yield break</c> → <c>raise Yielder::YieldBreak</c></item>
///         <item><c>yield return expr</c> → <c>{ raise Yielder::Yield{value: expr }; raise Yielder::YieldBreak }</c></item>
///     </list>
/// </remarks>
public sealed record YieldStatement : ValkyrieNode
{
    /// <summary>
    ///     Yield 关键字类型
    /// </summary>
    public YieldKeyword keyword { get; init; }

    /// <summary>
    ///     产出值，对于 <c>yield break</c> 为 <c>null</c>
    /// </summary>
    public TermNode? value { get; init; }
}