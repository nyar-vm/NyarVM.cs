using System.Globalization;
using Core.Command;

namespace Std.Command;

public sealed class LocalizableString
{
    public string key { get; init; } = string.Empty;
    public string default_value { get; init; } = string.Empty;

    public static implicit operator LocalizableString(string plain)
    {
        return new LocalizableString { key = plain, default_value = plain };
    }

    public string resolve(ILocalizer? localizer)
    {
        if (localizer is not null) return localizer.get_string(key, default_value);

        return default_value;
    }

    /// <summary>
    ///     根据指定的区域信息返回本地化字符串
    /// </summary>
    /// <param name="culture">目标区域信息</param>
    /// <returns>本地化后的字符串</returns>
    public string to_string(CultureInfo culture)
    {
        return Localizer.current?.get_string(key, default_value) ?? default_value;
    }

    public override string ToString()
    {
        return default_value;
    }
}