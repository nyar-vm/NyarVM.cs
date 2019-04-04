namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式 —— 匹配字面常量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// case module::Object { ... };
/// </code>
public sealed record PatternLiteralObjectNode : PatternNode
{
    public QualifiedPathNode? path { get; init; } = null;
    public IReadOnlyList<PatternLiteralFieldNode> fields { get; init; } = [];
}