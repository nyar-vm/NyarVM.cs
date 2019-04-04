namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ��̬���Խڵ�
/// </summary>
public sealed record WebStaticAttr : WebAttrNode
{
    /// <summary>
    ///     ��̬����ֵ
    /// </summary>
    public string value { get; set; } = string.Empty;
}