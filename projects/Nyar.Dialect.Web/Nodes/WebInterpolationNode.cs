namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ��ֵ���ʽ�ڵ� {expr}
/// </summary>
public sealed record WebInterpolationNode : WebIrNode
{
    /// <summary>
    ///     ���ʽ��ʶ��
    /// </summary>
    public string expr_id { get; set; } = string.Empty;
}