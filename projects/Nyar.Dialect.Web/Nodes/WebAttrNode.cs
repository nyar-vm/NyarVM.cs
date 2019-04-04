namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     HTML ���Խڵ����
/// </summary>
public abstract record WebAttrNode
{
    /// <summary>
    ///     ������
    /// </summary>
    public string name { get; set; } = string.Empty;
}