namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     �¼����Խڵ�
/// </summary>
public sealed record WebEventAttr : WebAttrNode
{
    /// <summary>
    ///     �¼�����
    /// </summary>
    public string event_type { get; set; } = string.Empty;

    /// <summary>
    ///     �¼���������ʶ��
    /// </summary>
    public string handler_id { get; set; } = string.Empty;
}