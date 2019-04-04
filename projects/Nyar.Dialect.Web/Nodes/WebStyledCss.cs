namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Scoped CSS ����
/// </summary>
public sealed class WebStyledCss
{
    /// <summary>
    ///     �������ʶ
    /// </summary>
    public string scope { get; set; } = string.Empty;

    /// <summary>
    ///     CSS ��ʽ����
    /// </summary>
    public string css { get; set; } = string.Empty;
}