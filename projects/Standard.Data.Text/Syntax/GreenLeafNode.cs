namespace Std.Data.Text.Syntax;

/// <summary>
///     绿树叶子节点，表示词法单元
/// </summary>
public sealed class GreenLeafNode : GreenNode
{
    /// <summary>
    ///     创建叶子节点
    /// </summary>
    public GreenLeafNode(NodeKind kind, int width, string? text = null)
    {
        this.kind = kind;
        this.width = width;
        this.text = text;
    }

    /// <inheritdoc />
    public override NodeKind kind { get; }

    /// <inheritdoc />
    public override int width { get; }

    /// <inheritdoc />
    public override int child_count => 0;

    /// <summary>
    ///     词法单元的文本内容
    /// </summary>
    public string? text { get; }

    /// <inheritdoc />
    public override GreenNode? get_child(int index)
    {
        return null;
    }

    /// <summary>
    ///     叶子节点直接写入文本
    /// </summary>
    public override void write_to(TextWriter writer)
    {
        if (text is not null) writer.Write(text);
    }
}