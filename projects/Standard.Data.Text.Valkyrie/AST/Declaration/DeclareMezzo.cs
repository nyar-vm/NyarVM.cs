namespace Std.Data.Text.Valkyrie.AST.Declaration;

public sealed record DeclareMezzo : ValkyrieNode, IDeclarationNode
{
    public Annotations annotations { get; init; } = new();
    public IdentifierNode? name { get; init; } = new();
}