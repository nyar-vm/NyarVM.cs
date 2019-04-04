namespace Core.Command;

/// <summary>
///     本地化器接口，提供基于键的资源字符串查找
/// </summary>
public interface ILocalizer
{
    /// <summary>
    ///     根据资源键获取本地化字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="fallback">回退值</param>
    /// <returns>本地化字符串，若找不到则返回回退值</returns>
    string get_string(string key, string? fallback = null);
}