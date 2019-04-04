using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     属性声明节点，表示 <c>[name(key = "value", ...)]</c> 形式的属性标注
/// </summary>
/// <para>属性可附加到变量、函数、类等声明上，用于元编程和编译指示</para>
/// <para>示例：</para>
/// <code>
/// [component]
/// [derive(Debug, Clone)]
/// let health: f32 = 100.0;
/// </code>
public sealed record AttributeItem : ValkyrieNode
{
    /// <summary>
    ///     属性名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     属性参数列表
    /// </summary>
    public TermArgumentList? arguments { get; init; } = null;
}