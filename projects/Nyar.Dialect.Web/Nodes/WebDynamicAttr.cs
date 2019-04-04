namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ��̬���Խڵ�
/// </summary>
public sealed record WebDynamicAttr : WebAttrNode
{
    /// <summary>
    ///     ��̬���ʽ��ʶ��
    /// </summary>
    public string expr_id { get; set; } = string.Empty;
}