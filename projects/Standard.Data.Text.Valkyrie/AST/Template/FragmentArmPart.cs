namespace Std.Data.Text.Valkyrie.AST.Template;

/// <summary>
///     虚拟节点
/// </summary>
public sealed record FragmentArmPart
{
    public FragmentArmNode arm { get; init; } = null!;
    public IReadOnlyList<ValkyrieNode> parts { get; init; } = [];
}