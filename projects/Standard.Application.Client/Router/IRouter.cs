namespace Std.App.Client;

/// <summary>
///     基于路径/URL 的页面导航路由器接口
/// </summary>
public interface IRouter
{
    /// <summary>
    ///     当前路径
    /// </summary>
    string CurrentPath { get; }

    /// <summary>
    ///     是否可以返回
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    ///     是否可以前进
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    ///     导航到指定路径
    /// </summary>
    /// <param name="path">目标路径</param>
    Task NavigateToAsync(string path);

    /// <summary>
    ///     返回上一页
    /// </summary>
    Task GoBackAsync();

    /// <summary>
    ///     前?到下一页
    /// </summary>
    Task GoForwardAsync();

    /// <summary>
    ///     路径变更事件
    /// </summary>
    event Action<string>? OnPathChanged;
}