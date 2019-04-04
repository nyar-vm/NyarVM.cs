namespace Std.Data.Text.Valkyrie.AST.Type;

public sealed record TypeLiteralNamePathNode : TypeNode
{
    public QualifiedPathNode path { get; init; }

    public TypeArgumentList? type_arguments { get; init; }
}
