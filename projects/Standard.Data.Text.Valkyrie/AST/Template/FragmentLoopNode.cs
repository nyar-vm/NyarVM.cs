using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     loop 片段节点，表示循环模板的起始标记
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% loop pattern in expression %>
/// </code>
public sealed record FragmentLoopNode : ValkyrieNode
{
    private PatternNode? _pattern { get; init; }
    private TermNode? _expression { get; init; }
    private TermNode? _guard { get; init; }
}