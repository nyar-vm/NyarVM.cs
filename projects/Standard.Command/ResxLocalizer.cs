using System.Globalization;
using Core.Command;

namespace Std.Command;

/// <summary>
///     基于内存词典的本地化器实现，支持运行时添加多语言资源
/// </summary>
public sealed class ResxLocalizer : ILocalizer
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string get_string(string key, string? fallback = null)
    {
        var culture = CultureInfo.CurrentUICulture;
        return get_string(key, culture, fallback);
    }

    /// <summary>
    ///     添加单条本地化资源
    /// </summary>
    /// <param name="cultureName">文化名称，如 "zh-CN"、"en-US"</param>
    /// <param name="key">资源键</param>
    /// <param name="value">本地化值</param>
    public void add_resource(string cultureName, string key, string value)
    {
        if (!_resources.TryGetValue(cultureName, out var entries))
        {
            entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _resources[cultureName] = entries;
        }

        entries[key] = value;
    }

    /// <summary>
    ///     批量添加本地化资源
    /// </summary>
    /// <param name="cultureName">文化名称</param>
    /// <param name="entries">键值对集合</param>
    public void add_resources(string cultureName, IEnumerable<KeyValuePair<string, string>> entries)
    {
        if (!_resources.TryGetValue(cultureName, out var dict))
        {
            dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _resources[cultureName] = dict;
        }

        foreach (var (key, value) in entries) dict[key] = value;
    }

    /// <summary>
    ///     根据指定文化获取本地化字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="culture">文化</param>
    /// <param name="fallback">回退值</param>
    /// <returns>本地化字符串</returns>
    public string get_string(string key, CultureInfo culture, string? fallback = null)
    {
        var cultureName = culture.Name;

        if (_resources.TryGetValue(cultureName, out var entries) && entries.TryGetValue(key, out var value))
            return value;

        if (cultureName.Contains('-'))
        {
            var neutral = cultureName.Split('-')[0];
            if (_resources.TryGetValue(neutral, out var neutralEntries) &&
                neutralEntries.TryGetValue(key, out var neutralValue)) return neutralValue;
        }

        return fallback ?? key;
    }
}