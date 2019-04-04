namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     �ı��ڵ�
/// </summary>
public sealed record WebTextNode : WebIrNode
{
    /// <summary>
    ///     �ı�����
    /// </summary>
    public string text { get; set; } = string.Empty;
}