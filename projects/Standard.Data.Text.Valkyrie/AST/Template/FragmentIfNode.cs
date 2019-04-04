using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     if 片段节点，表示条件模板的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% if condition %>
/// </code>
public sealed record FragmentIfNode : ValkyrieNode
{
    private TermNode _condition;

    public FragmentIfNode(TermNode condition)
    {
        _condition = condition;
    }
}