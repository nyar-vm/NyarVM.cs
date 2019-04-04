namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Props �ֶ�
/// </summary>
public sealed class WebPropField
{
    /// <summary>
    ///     �ֶ�����
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     �ֶ�����
    /// </summary>
    public string type { get; set; } = "string";

    /// <summary>
    ///     Ĭ��ֵ
    /// </summary>
    public string? default_value { get; set; }

    /// <summary>
    ///     �Ƿ����
    /// </summary>
    public bool required { get; set; }
}