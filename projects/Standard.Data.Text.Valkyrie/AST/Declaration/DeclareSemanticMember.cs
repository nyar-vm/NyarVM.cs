using Std.Data.Text.Valkyrie.AST.Term;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     枚举成员声明，可携带可选的整数值
/// </summary>
/// <para>示例：</para>
/// <code>
/// enums Color {
///     Red,          // Name = "Red", Value = null（自动递增）
///     Green = 5,    // Name = "Green", Value = LiteralExpr(5)
///     Blue          // Name = "Blue", Value = null（自动递增为 6）
/// }
/// </code>
public sealed record DeclareSemanticMember : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     成员值（可选的常量表达式，为空时自动递增）
    /// </summary>
    public TermNode? value { get; init; }

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     成员名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}