namespace Nyar.Dialect.Web.Nodes;

/// <summary>
///     Web �� GGScript �м��ʾ��IR���Ķ�����뵥Ԫ
/// </summary>
public sealed class WebCompilationUnit
{
    /// <summary>
    ///     ��������б�
    /// </summary>
    public List<WebComponentDecl> components { get; set; } = [];

    /// <summary>
    ///     ȫ������
    /// </summary>
    public WebConfigDecl? config { get; set; }

    /// <summary>
    ///     ·���嵥
    /// </summary>
    public WebRouteManifest? routes { get; set; }
}