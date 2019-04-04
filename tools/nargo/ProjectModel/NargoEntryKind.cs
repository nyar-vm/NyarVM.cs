namespace Nargo.Cli.ProjectModel;

/// <summary>
///     入口类型
/// </summary>
public enum NargoEntryKind
{
    /// <summary>
    ///     HTML 入口
    /// </summary>
    Html,

    /// <summary>
    ///     TypeScript / JavaScript 入口
    /// </summary>
    Script,

    /// <summary>
    ///     SSR 入口
    /// </summary>
    Ssr,

    /// <summary>
    ///     Worker 入口
    /// </summary>
    Worker,

    /// <summary>
    ///     CSS 入口
    /// </summary>
    Css,

    /// <summary>
    ///     测试入口
    /// </summary>
    Test,
}