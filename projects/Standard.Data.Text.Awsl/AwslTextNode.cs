namespace Std.Data.Text.Awsl;

/// <summary>
///     文本节点
/// </summary>
public sealed class AwslTextNode : AwslTemplateNode
{
    public string text { get; init; } = string.Empty;
}