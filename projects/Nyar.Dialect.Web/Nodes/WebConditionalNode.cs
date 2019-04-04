namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ������Ⱦ�ڵ� #if
/// </summary>
public sealed record WebConditionalNode : WebIrNode
{
    /// <summary>
    ///     �������ʽ��ʶ��
    /// </summary>
    public string cond_expr_id { get; set; } = string.Empty;

    /// <summary>
    ///     ����Ϊ��ʱ��Ⱦ�Ľڵ��б�
    /// </summary>
    public List<WebIrNode> then_nodes { get; set; } = [];

    /// <summary>
    ///     ����Ϊ��ʱ��Ⱦ�Ľڵ��б�
    /// </summary>
    public List<WebIrNode> else_nodes { get; set; } = [];
}