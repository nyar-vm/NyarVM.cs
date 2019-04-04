using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     case 分支片段节点，表示匹配分支的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% case pattern %>
/// </code>
public sealed record FragmentArmCaseNode : FragmentArmNode
{
    public PatternNode pattern { get; init; }

    public TermNode? guard { get; init; }
}