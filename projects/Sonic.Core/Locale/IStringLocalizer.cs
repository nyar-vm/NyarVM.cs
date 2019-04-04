namespace Core.Locale;

/// <summary>
///     字符串本地化器接口，提供本地化字符串的格式化获取能力
/// </summary>
public interface IStringLocalizer
{
    /// <summary>
    ///     根据名称获取本地化字符串，支持格式化参数
    /// </summary>
    /// <param name="name">字符串名称</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的本地化字符串</returns>
    string get(string name, params object[] args);
}