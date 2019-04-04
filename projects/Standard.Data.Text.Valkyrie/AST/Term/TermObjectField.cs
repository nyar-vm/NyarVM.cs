namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     对象构造字段。
/// </summary>
public sealed record TermObjectField : ValkyrieNode
{
    /// <summary>
    ///     字段名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     字段值，省略时表示简写绑定
    /// </summary>
    public ValkyrieNode? value { get; init; }
}