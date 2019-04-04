namespace Std.Data.Text.Awsl;

/// <summary>
///     条件节点
/// </summary>
public sealed class AwslIfNode : AwslTemplateNode
{
    public string condition { get; init; } = "true";

    public IReadOnlyList<AwslTemplateNode> children { get; init; } = [];

    public IReadOnlyList<AwslTemplateNode> else_children { get; init; } = [];
}