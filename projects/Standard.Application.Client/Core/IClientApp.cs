namespace Std.App.Client;

/// <summary>
///     客户端应用入口接口，管理应用生命周期（启动、暂停、恢复、退出）
/// </summary>
public interface IClientApp
{
    /// <summary>
    ///     当前生命周期阶段
    /// </summary>
    AppLifecycle Lifecycle { get; }

    /// <summary>
    ///     启动应用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     暂停应用（如移动端切到后台）
    /// </summary>
    Task PauseAsync();

    /// <summary>
    ///     从暂停状态恢复
    /// </summary>
    Task ResumeAsync();

    /// <summary>
    ///     停止应用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}