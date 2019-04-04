namespace Std.Data.Text.Valkyrie.AST.Term;

public sealed record TermLiteralNumberNode : TermNode
{
    /// <summary>
    ///     数字字面量的原始文本。
    /// </summary>
    public string value { get; init; } = "0";
}