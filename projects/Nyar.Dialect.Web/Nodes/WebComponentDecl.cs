namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Web �������
/// </summary>
public sealed class WebComponentDecl
{
    /// <summary>
    ///     �������
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     ��� Props ����
    /// </summary>
    public WebPropsDecl props { get; set; } = new();

    /// <summary>
    ///     IR �ڵ��б�
    /// </summary>
    public List<WebIrNode> nodes { get; set; } = [];

    /// <summary>
    ///     Signal �����б�
    /// </summary>
    public List<WebSignalDecl> signals { get; set; } = [];

    /// <summary>
    ///     Scoped CSS �б�
    /// </summary>
    public List<WebStyledCss> styled_css { get; set; } = [];

    /// <summary>
    ///     Island �����б�
    /// </summary>
    public List<WebIslandDecl> islands { get; set; } = [];
}