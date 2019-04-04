namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 元数据，对齐 pandoc Meta
/// </summary>
public sealed class Meta
{
    /// <summary>
    ///     元数据键值对
    /// </summary>
    public IReadOnlyDictionary<string, MetaValue> values { get; init; } = new Dictionary<string, MetaValue>();

    /// <summary>
    ///     空元数据
    /// </summary>
    public static Meta empty { get; } = new();

    /// <summary>
    ///     获取字符串元数据值
    /// </summary>
    public string? get_string(string key)
    {
        if (values.TryGetValue(key, out var value) && value is MetaValue.MetaString str) return str.value;

        return null;
    }

    /// <summary>
    ///     设置字符串元数据值
    /// </summary>
    public Meta with_value(string key, MetaValue value)
    {
        var dict = new Dictionary<string, MetaValue>(values) { [key] = value };
        return new Meta { values = dict };
    }

    /// <summary>
    ///     设置字符串元数据值
    /// </summary>
    public Meta with_value(string key, string value)
    {
        return with_value(key, MetaValue.from_string(value));
    }

    /// <summary>
    ///     设置布尔元数据值
    /// </summary>
    public Meta with_value(string key, bool value)
    {
        return with_value(key, MetaValue.from_bool(value));
    }
}