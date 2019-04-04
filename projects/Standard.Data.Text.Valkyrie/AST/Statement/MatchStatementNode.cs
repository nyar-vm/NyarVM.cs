using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Match 模式匹配语句，对表达式值进行多分支模式匹配
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     case 0:
///         print("零");
///     case 1 | 2:
///         print("一或二");
///     case n if n &gt; 0:
///         print("正数 {n}");
///     case _:
///         print("未知");
/// }
/// </code>
public sealed record MatchStatementNode : ValkyrieNode
{
    /// <summary>
    ///     被匹配的表达式
    /// </summary>
    public TermNode expression { get; init; }

    /// <summary>
    ///     匹配分支列表
    /// </summary>
    public IReadOnlyList<ArmNode> arms { get; init; } = [];
}