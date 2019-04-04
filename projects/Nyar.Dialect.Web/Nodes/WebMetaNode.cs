namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Meta ��ǩ�ڵ㣨Head / Script��
/// </summary>
public sealed record WebMetaNode : WebIrNode
{
    /// <summary>
    ///     Meta ����
    /// </summary>
    public WebMetaKind kind { get; set; }

    /// <summary>
    ///     ���ݽڵ��б�
    /// </summary>
    public List<WebIrNode> content_nodes { get; set; } = [];
}