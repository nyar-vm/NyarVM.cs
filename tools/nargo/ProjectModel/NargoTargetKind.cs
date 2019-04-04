namespace Nargo.Cli.ProjectModel;

/// <summary>
///     工程目标类型
/// </summary>
public enum NargoTargetKind
{
    /// <summary>
    ///     浏览器应用
    /// </summary>
    BrowserApp,

    /// <summary>
    ///     浏览器库
    /// </summary>
    BrowserLib,

    /// <summary>
    ///     Node 应用
    /// </summary>
    NodeApp,

    /// <summary>
    ///     Node 库
    /// </summary>
    NodeLib,

    /// <summary>
    ///     SSR 应用
    /// </summary>
    SsrApp,

    /// <summary>
    ///     Worker
    /// </summary>
    Worker,

    /// <summary>
    ///     同构库
    /// </summary>
    IsomorphicLib,

    /// <summary>
    ///     CLI 工具
    /// </summary>
    Cli,
}