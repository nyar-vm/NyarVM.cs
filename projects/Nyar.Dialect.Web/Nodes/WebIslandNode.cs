namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Island �ܹ��ڵ�
/// </summary>
public sealed record WebIslandNode : WebIrNode
{
    /// <summary>
    ///     Island ����
    /// </summary>
    public WebIslandKind kind { get; set; }

    /// <summary>
    ///     ���������
    /// </summary>
    public string component_ref { get; set; } = string.Empty;

    /// <summary>
    ///     ��������ֵ�
    /// </summary>
    public Dictionary<string, string> props { get; set; } = [];
}