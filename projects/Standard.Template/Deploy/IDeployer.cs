namespace Std.Template.Deploy;

/// <summary>
///     部署器接口
/// </summary>
public interface IDeployer
{
    /// <summary>
    ///     部署 public/ 目录到目标
    /// </summary>
    /// <param name="sourceDir">源目录路径</param>
    /// <param name="options">部署选项</param>
    /// <param name="ct">取消令牌</param>
    Task DeployAsync(string sourceDir, DeployOptions options, CancellationToken ct = default);
}