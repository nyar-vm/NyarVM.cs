using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     元信息声明，类或 Shader 中的 <c>meta</c> 配置段
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% when condition %>
///         <% when condition if guard %>
/// </code>
public sealed record FragmentArmWhenNode : FragmentArmNode
{
    public TermNode term { get; init; }

    public TermNode? guard { get; init; }
}