namespace Std.Data.Text.Valkyrie.AST.Neural;

/// <summary>
///     Repeater 重复层声明，用于将子层重复指定次数
/// </summary>
public sealed record RepeaterDecl : ValkyrieNode
{
    /// <summary>
    ///     重复次数
    /// </summary>
    public int count { get; init; }

    /// <summary>
    ///     需要重复的子层列表
    /// </summary>
    public IReadOnlyList<ValkyrieNode> body { get; init; } = [];
}