namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     ȫ����������
/// </summary>
public sealed class WebConfigDecl
{
    /// <summary>
    ///     �Ƿ����� PWA
    /// </summary>
    public bool pwa_enabled { get; set; }

    /// <summary>
    ///     �Ƿ�������ģ���滻
    /// </summary>
    public bool hmr_enabled { get; set; }

    /// <summary>
    ///     �Ƿ����÷������Ⱦ
    /// </summary>
    public bool ssr_enabled { get; set; }
}