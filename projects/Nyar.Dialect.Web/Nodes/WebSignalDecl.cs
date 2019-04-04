namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ��Ӧʽ Signal ����
/// </summary>
public sealed class WebSignalDecl
{
    /// <summary>
    ///     Signal ����
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     Signal ����
    /// </summary>
    public string type { get; set; } = "any";

    /// <summary>
    ///     ��ʼֵ
    /// </summary>
    public string? initial_value { get; set; }

    /// <summary>
    ///     �Ƿ�Ϊ��������
    /// </summary>
    public bool is_computed { get; set; }
}