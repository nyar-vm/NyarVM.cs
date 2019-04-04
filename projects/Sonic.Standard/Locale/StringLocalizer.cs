using Core.Locale;

namespace Std.Locale;

/// <summary>
///     字符串本地化器，提供本地化字符串的格式化获取能力。
/// </summary>
public sealed class StringLocalizer : IStringLocalizer
{
    private readonly string _default_locale;
    private readonly Dictionary<string, string> _strings;

    /// <summary>
    ///     初始化 <see cref="StringLocalizer" /> 的新实例。
    /// </summary>
    /// <param name="strings">初始字符串字典。</param>
    /// <param name="defaultLocale">默认区域，默认为 "en"。</param>
    public StringLocalizer(Dictionary<string, string>? strings = null, string defaultLocale = "en")
    {
        _strings = strings ?? new Dictionary<string, string>();
        _default_locale = defaultLocale;
    }

    /// <summary>
    ///     根据名称获取本地化字符串的索引器。
    /// </summary>
    /// <param name="name">字符串名称。</param>
    /// <returns>本地化字符串。</returns>
    public string this[string name] => get(name);

    /// <summary>
    ///     根据名称获取本地化字符串，支持格式化参数。
    /// </summary>
    /// <param name="name">字符串名称。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>格式化后的本地化字符串。</returns>
    public string get(string name, params object[] args)
    {
        var value = _strings.GetValueOrDefault(name, name);

        if (args is null || args.Length == 0) return value;

        return string.Format(value, args);
    }

    /// <summary>
    ///     添加或更新本地化字符串。
    /// </summary>
    /// <param name="name">字符串名称。</param>
    /// <param name="value">本地化值。</param>
    public void add(string name, string value)
    {
        _strings[name] = value;
    }

    /// <summary>
    ///     批量添加本地化字符串。
    /// </summary>
    /// <param name="entries">字符串键值对集合。</param>
    public void add_range(IEnumerable<KeyValuePair<string, string>> entries)
    {
        foreach (var entry in entries) _strings[entry.Key] = entry.Value;
    }
}