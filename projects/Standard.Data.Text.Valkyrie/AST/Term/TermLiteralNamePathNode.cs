namespace Std.Data.Text.Valkyrie.AST.Term;

public sealed record TermLiteralNamePathNode : TermNode
{
    public QualifiedPathNode path { get; init; }
}