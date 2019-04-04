using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     type 片段节点，表示类型模板的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% type Typing %>
///         <% type i32 if x> 42 %>
/// </code>
public sealed record FragmentArmTypeNode : FragmentArmNode
{
    public TypeNode type { get; init; }

    public TermNode? guard { get; init; }
}