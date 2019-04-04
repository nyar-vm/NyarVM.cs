namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     HTML Ԫ�ؽڵ�
/// </summary>
public sealed record WebElementNode : WebIrNode
{
    /// <summary>
    ///     HTML ��ǩ��
    /// </summary>
    public string tag { get; set; } = "div";

    /// <summary>
    ///     �����б�
    /// </summary>
    public List<WebAttrNode> attrs { get; set; } = [];

    /// <summary>
    ///     �ӽڵ��б�
    /// </summary>
    public List<WebIrNode> children { get; set; } = [];

    /// <summary>
    ///     �ڵ��ʶ��
    /// </summary>
    public int node_id { get; set; }
}