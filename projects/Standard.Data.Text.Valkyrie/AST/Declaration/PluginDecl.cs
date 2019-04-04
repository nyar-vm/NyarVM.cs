namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     Plugin 声明节点，如 <c>plugin Logger { ... }</c>
///     定义一组函数的插件契约
/// </summary>
public sealed record PluginDecl : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     Plugin 中包含的函数列表
    /// </summary>
    public IReadOnlyList<DeclareMicro> functions { get; init; } = [];

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     Plugin 名称
    /// </summary>
    public IdentifierNode? name { get; init; }
}