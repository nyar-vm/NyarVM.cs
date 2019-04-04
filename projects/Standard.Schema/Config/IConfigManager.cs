namespace Hermes.Config;

/// <summary>
///     Config 管理器 — 解析、验证、监控配置
/// </summary>
public interface IConfigManager
{
    /// <summary>
    ///     解析配置为强类型对象
    /// </summary>
    /// <typeparam name="T">Config 类型</typeparam>
    /// <returns>解析后的强类型配置对象</returns>
    /// <exception cref="ConfigValidationException">验证失败时抛出</exception>
    Task<T> ResolveAsync<T>() where T : class;

    /// <summary>
    ///     获取配置快照（热重载感知）
    /// </summary>
    /// <typeparam name="T">Config 类型</typeparam>
    IConfigSnapshot<T> GetSnapshot<T>() where T : class;

    /// <summary>
    ///     订阅配置变更
    /// </summary>
    /// <typeparam name="T">Config 类型</typeparam>
    /// <param name="onChange">变更回调</param>
    IDisposable Watch<T>(Action<T> onChange) where T : class;

    /// <summary>
    ///     获取超级机密字段的值（仅走加密通道）
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="path">机密路径，如 "app.master_key"</param>
    Task<T> GetSuperSecretAsync<T>(string path);

    /// <summary>
    ///     启动时验证所有已注册的 config
    /// </summary>
    /// <returns>验证结果列表</returns>
    Task<IReadOnlyList<ConfigValidationError>> ValidateAllAsync();
}