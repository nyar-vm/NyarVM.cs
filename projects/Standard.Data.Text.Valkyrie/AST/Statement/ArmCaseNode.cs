using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Match 匹配分支，由模式和对应代码体组成
/// </summary>
/// <para>示例：</para>
/// <code>
/// match value {
///     case 0:
///         print("零");
///     case n if n &gt; 0:
///         print("正数");
///     case _:
///         print("负数");
/// }
/// </code>
public sealed record ArmCaseNode : ArmNode
{
    public TermNode? guard;

    /// <summary>
    ///     匹配模式（<see cref="PatternLiteralNumberNode" /> / <see cref="DeclarationPattern" /> / <see cref="PatternNode" /> /
    ///     <see cref="PatternLiteralWildcardNode" />）
    /// </summary>
    public PatternNode pattern { get; init; }
}