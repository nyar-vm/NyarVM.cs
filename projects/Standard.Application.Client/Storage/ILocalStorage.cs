namespace Std.App.Client;

/// <summary>
///     本地存储抽象接口，提供键值存储能力
/// </summary>
public interface ILocalStorage
{
    /// <summary>
    ///     获取指定键的值
    /// </summary>
    /// <param name="key">存储键</param>
    Task<string?> GetAsync(string key);

    /// <summary>
    ///     设置指定键的值
    /// </summary>
    /// <param name="key">存储键</param>
    /// <param name="value">存储值</param>
    Task SetAsync(string key, string value);

    /// <summary>
    ///     删除指定键
    /// </summary>
    /// <param name="key">存储键</param>
    Task RemoveAsync(string key);

    /// <summary>
    ///     清空所有存储
    /// </summary>
    Task ClearAsync();

    /// <summary>
    ///     检查键是否存在
    /// </summary>
    /// <param name="key">存储键</param>
    Task<bool> ContainsKeyAsync(string key);
}