namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Island ����
/// </summary>
public sealed class WebIslandDecl
{
    /// <summary>
    ///     �������
    /// </summary>
    public string component_name { get; set; } = string.Empty;

    /// <summary>
    ///     Island ����
    /// </summary>
    public WebIslandKind kind { get; set; }

    /// <summary>
    ///     ˮ�ϲ���
    /// </summary>
    public WebHydrationStrategy hydration { get; set; } = WebHydrationStrategy.eager;
}