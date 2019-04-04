namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Suspense �첽���ؽڵ�
/// </summary>
public sealed record WebSuspenseNode : WebIrNode
{
    /// <summary>
    ///     ����ʱռλ�ڵ��б�
    /// </summary>
    public List<WebIrNode> fallback_nodes { get; set; } = [];

    /// <summary>
    ///     �첽���ݽڵ��б�
    /// </summary>
    public List<WebIrNode> content_nodes { get; set; } = [];
}