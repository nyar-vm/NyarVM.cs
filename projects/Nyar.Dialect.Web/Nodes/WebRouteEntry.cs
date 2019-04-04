namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ·����Ŀ
/// </summary>
public sealed class WebRouteEntry
{
    /// <summary>
    ///     ·��·��
    /// </summary>
    public string path { get; set; } = string.Empty;

    /// <summary>
    ///     ���������
    /// </summary>
    public string component { get; set; } = string.Empty;

    /// <summary>
    ///     �Ƿ�Ϊ��̬·��
    /// </summary>
    public bool is_dynamic { get; set; }

    /// <summary>
    ///     ��̬·�ɲ����б�
    /// </summary>
    public List<string> @params { get; set; } = [];
}