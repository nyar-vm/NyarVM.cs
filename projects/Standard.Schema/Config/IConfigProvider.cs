namespace Hermes.Config;

/// <summary>
///     Config Provider 接口 — 从特定 source 加载原始配置值
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    ///     Provider 名称（对应 source:entity 中的 entity 部分，如 "appsettings"、"app"）
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Provider 类型（对应 source:entity 中的 source 部分，如 "json"、"env"、"consul"）
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    ///     尝试加载指定路径的原始配置值
    /// </summary>
    /// <param name="path">配置路径，如 "AppConfig.max_retry"</param>
    /// <returns>配置值，如果不存在返回 null</returns>
    Task<string?> TryLoadAsync(string path);

    /// <summary>
    ///     加载提供的所有原始值的快照
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> LoadAllAsync();

    /// <summary>
    ///     订阅配置变更
    /// </summary>
    /// <param name="callback">变更回调函数</param>
    /// <returns>取消订阅的句柄</returns>
    IDisposable Watch(Action<IReadOnlyDictionary<string, string>> callback);
}