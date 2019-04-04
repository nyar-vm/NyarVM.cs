namespace Std.Data.Text.Valkyrie.AST.Term;

public sealed record TermLiteralBooleanNode : TermNode
{
    /// <summary>
    ///     布尔字面量值。
    /// </summary>
    public bool value { get; init; }
}