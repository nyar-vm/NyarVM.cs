using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     关联类型声明，如 <c>type View: TextView&lt;Text = Self&gt;;</c> 或 <c>type Resume = i32</c>
/// </summary>
public sealed record DeclareAssociatedType : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     约束类型（<c>:</c> 语法），如 <c>type View: TextView</c>
    /// </summary>
    public WhereConstraintNode? constraint { get; init; }

    /// <summary>
    ///     默认类型（<c>=</c> 语法），如 <c>type Resume = i32</c>
    /// </summary>
    public TypeNode? default_type { get; init; }

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     关联类型名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}