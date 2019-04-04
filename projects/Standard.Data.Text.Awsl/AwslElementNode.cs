namespace Std.Data.Text.Awsl;

/// <summary>
///     元素节点
/// </summary>
public sealed class AwslElementNode : AwslTemplateNode
{
    public string tag_name { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> attributes { get; init; } = new Dictionary<string, string>();

    public IReadOnlyList<AwslTemplateNode> children { get; init; } = [];

    public bool is_self_closing { get; init; }
}