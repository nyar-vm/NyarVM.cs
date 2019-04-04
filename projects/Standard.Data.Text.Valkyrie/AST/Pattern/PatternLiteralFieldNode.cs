namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式字段 —— 匹配对象字段
/// </summary>
public sealed record PatternLiteralFieldNode : ValkyrieNode
{
    public string name { get; init; } = null!;
    public PatternNode pattern { get; init; } = null!;
}