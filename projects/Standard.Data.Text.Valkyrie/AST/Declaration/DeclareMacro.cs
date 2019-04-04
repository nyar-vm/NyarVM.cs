namespace Std.Data.Text.Valkyrie.AST.Declaration;

public sealed record DeclareMacro : ValkyrieNode, IDeclarationNode
{
    public Annotations annotations { get; init; } = new();
    public IdentifierNode? name { get; init; } = new();
}