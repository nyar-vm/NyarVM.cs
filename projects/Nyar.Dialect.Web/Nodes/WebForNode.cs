namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     �б���Ⱦ�ڵ� #for
/// </summary>
public sealed record WebForNode : WebIrNode
{
    /// <summary>
    ///     ѭ��������
    /// </summary>
    public string var_name { get; set; } = string.Empty;

    /// <summary>
    ///     �ɵ���������ʽ��ʶ��
    /// </summary>
    public string iterable_expr_id { get; set; } = string.Empty;

    /// <summary>
    ///     ѭ����ڵ��б�
    /// </summary>
    public List<WebIrNode> body_nodes { get; set; } = [];

    /// <summary>
    ///     Key ���ʽ��ʶ��
    /// </summary>
    public string? key_expr_id { get; set; }
}