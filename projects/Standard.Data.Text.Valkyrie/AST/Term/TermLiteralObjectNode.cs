namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     对象或变体构造表达式。
/// </summary>
/// <para>示例：</para>
/// <code>
/// Some { value: x }
/// Self { map: HashMap.new(16) }
/// </code>
public sealed record TermLiteralObjectNode : TermNode
{
    /// <summary>
    ///     构造目标
    /// </summary>
    public ValkyrieNode constructor { get; init; } = new IdentifierNode();

    /// <summary>
    ///     字段初始化列表
    /// </summary>
    public IReadOnlyList<TermObjectField> fields { get; init; } = [];

    /// <summary>
    ///     是否包含展开匹配 `..`
    /// </summary>
    public bool has_spread { get; init; }
}