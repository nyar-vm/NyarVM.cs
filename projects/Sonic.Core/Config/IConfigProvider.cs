namespace Core.Config;

/// <summary>
///     配置提供者接口，提供配置键值的查询能力
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    ///     根据键获取配置值
    /// </summary>
    /// <param name="key">配置键</param>
    /// <returns>配置值，键不存在时返回 null</returns>
    string? get(string key);

    /// <summary>
    ///     尝试根据键获取配置值
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="value">配置值，键不存在时为 null</param>
    /// <returns>键是否存在</returns>
    bool try_get(string key, out string? value);
}