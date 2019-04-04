using Core.Command;

namespace Std.Command;

/// <summary>
///     全局本地化器 Ambient Context，提供整个应用程序的本地化入口
/// </summary>
public static class Localizer
{
    /// <summary>
    ///     当前本地化器实例
    /// </summary>
    public static ILocalizer? current { get; set; }

    /// <summary>
    ///     通过当前本地化器获取字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="fallback">回退值</param>
    /// <returns>本地化字符串</returns>
    public static string? get_string(string key, string? fallback = null)
    {
        return current?.get_string(key, fallback) ?? fallback;
    }
}