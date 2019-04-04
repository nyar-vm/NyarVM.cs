namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     属性参数键值对，表示 <c>#[attr(key = "value")]</c> 中的单个参数
/// </summary>
/// <para>示例：</para>
/// <code>
/// f(value)
/// f([attribute] value)
/// f([attribute] x: value)
/// f(modifier x: value)
/// f([attribute] modifier x: value)
/// </code>
public sealed record TermArgumentItem : ValkyrieNode
{
    /// <summary>
    ///     参数键名
    /// </summary>
    public IDeclarationNode? key { get; init; } = null;

    /// <summary>
    ///     参数值，默认为 <c>"true"</c>（支持无值属性如 <c>[hidden]</c>）
    /// </summary>
    public TermNode value { get; init; } = null;
}