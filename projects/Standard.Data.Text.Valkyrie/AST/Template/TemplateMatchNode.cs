namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     元 match 语句，meta 代码中的多分支匹配选择
/// </summary>
/// <para>示例：</para>
/// <code>
/// <% match expr %>
///         <% case pattern1 %>
///             body1
///             <% case pattern2 %>
///                 body2
///                 <% else %>
///                     default
///                     <% end match %>
/// </code>
public sealed record TemplateMatchNode : ValkyrieNode
{
    public TemplateMatchNode(FragmentMatchNode begin, IReadOnlyList<FragmentArmPart> armParts)
    {
        this.begin = begin;
        arm_parts = armParts;
    }

    /// <summary>
    ///     被匹配的值表达式 AST
    /// </summary>
    public FragmentMatchNode begin { get; }

    public IReadOnlyList<FragmentArmPart> arm_parts { get; }
}