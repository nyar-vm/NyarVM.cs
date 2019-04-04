namespace Std.Data.Text.Awsl;

/// <summary>
///     循环节点
/// </summary>
public sealed class AwslForNode : AwslTemplateNode
{
    public string iterator { get; init; } = "item";

    public string iterable { get; init; } = "items";

    public IReadOnlyList<AwslTemplateNode> children { get; init; } = [];
}