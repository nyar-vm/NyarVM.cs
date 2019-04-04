namespace Std.Data.Text.Awsl;

/// <summary>
///     AWSL 元素属性，支持普通属性、事件绑定和数据绑定
/// </summary>
public sealed class WebAttribute
{
    /// <summary>
    ///     属性名称，含 @ 前缀
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     属性值（原始字符串或表达式）
    /// </summary>
    public string? value { get; init; }

    /// <summary>
    ///     属性种类
    /// </summary>
    public WebAttributeKind kind { get; init; }

    /// <summary>
    ///     是否为布尔属性（无值）
    /// </summary>
    public bool is_boolean_attribute { get; init; }

    /// <summary>
    ///     转换为键值对，用于兼容 Dictionary&lt;string, string&gt; 接口
    /// </summary>
    /// <returns>含前缀的属性名和值。</returns>
    public KeyValuePair<string, string> to_key_value_pair()
    {
        var prefix = kind switch
        {
            WebAttributeKind.event_binding => "@",
            WebAttributeKind.data_binding => "@",
            _ => string.Empty
        };

        return new KeyValuePair<string, string>(
            $"{prefix}{name}",
            value ?? (is_boolean_attribute ? "true" : string.Empty));
    }
}