using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     对象方法声明节点
/// </summary>
public sealed record DeclareObjectMethod : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<TermParameterList> parameters { get; init; } = [];

    /// <summary>
    ///     返回值类型注解
    /// </summary>
    public TypeNode? return_type { get; init; }

    /// <summary>
    ///     方法体
    /// </summary>
    public FunctionBody? body { get; init; }

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     方法名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}