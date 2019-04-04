namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     变量模式 —— 匹配任意值并将其绑定到变量
/// </summary>
public sealed record PatternLiteralVariableNode : PatternNode
{
    public string name { get; init; } = null!;
}