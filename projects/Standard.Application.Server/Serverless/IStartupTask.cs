namespace Std.App.Server.Serverless;

/// <summary>
///     启动任务抽象接口，在 DI 容器构建后、处理请求前执行预热逻辑
/// </summary>
public interface IStartupTask
{
    /// <summary>
    ///     执行启动预热任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task execute(CancellationToken cancellationToken = default);
}