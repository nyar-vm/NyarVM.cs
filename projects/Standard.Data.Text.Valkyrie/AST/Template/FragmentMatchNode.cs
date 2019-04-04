using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     match 片段节点，表示匹配模板的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% match expression %>
///         <% match expression if guard %>
/// </code>
public sealed record FragmentMatchNode : ValkyrieNode
{
    public FragmentMatchNode(TermNode expression)
    {
        this.expression = expression;
    }

    public TermNode expression { get; }

    public TermNode? guard { get; init; }
}