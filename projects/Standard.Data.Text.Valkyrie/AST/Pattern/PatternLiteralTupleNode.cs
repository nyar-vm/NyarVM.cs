namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式 —— 匹配字面常量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// case module::Tuple(...);
/// </code>
public sealed record PatternLiteralTupleNode : PatternNode
{
    public QualifiedPathNode? path { get; init; } = null;
    public IReadOnlyList<PatternNode> elements { get; init; } = [];
}