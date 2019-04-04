namespace Core.Widget;

/// <summary>
///     INavigationService 接口
/// </summary>
public interface INavigationService
{
    /// <summary>
    ///     导航到指定路由
    /// </summary>
    void navigate_to(string route);

    /// <summary>
    ///     返回上一页
    /// </summary>
    void go_back();
}