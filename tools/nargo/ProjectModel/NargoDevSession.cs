namespace Nargo.Cli.ProjectModel;

/// <summary>
///     Nargo 开发会话：表达 dev server 的实时执行状态
/// </summary>
public sealed class NargoDevSession
{
    /// <summary>
    ///     会话标识
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     关联的项目
    /// </summary>
    public NargoProject? Project { get; init; }

    /// <summary>
    ///     是否启用 SSR 模式
    /// </summary>
    public bool SsrEnabled { get; init; }

    /// <summary>
    ///     开发服务器主机地址
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    ///     开发服务器端口
    /// </summary>
    public int Port { get; init; } = 3000;

    /// <summary>
    ///     会话是否正在运行
    /// </summary>
    public bool IsRunning { get; set; }
}