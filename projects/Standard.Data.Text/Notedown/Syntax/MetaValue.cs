namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     Notedown 元数据值，支持字符串、布尔、列表、映射四种类型
///     对齐 pandoc MetaValue
/// </summary>
public abstract record MetaValue
{
    /// <summary>
    ///     创建字符串元数据
    /// </summary>
    public static MetaValue from_string(string value)
    {
        return new MetaString(value);
    }

    /// <summary>
    ///     创建布尔元数据
    /// </summary>
    public static MetaValue from_bool(bool value)
    {
        return new MetaBool(value);
    }

    /// <summary>
    ///     字符串元数据值
    /// </summary>
    public sealed record MetaString(string value) : MetaValue;

    /// <summary>
    ///     布尔元数据值
    /// </summary>
    public sealed record MetaBool(bool value) : MetaValue;

    /// <summary>
    ///     列表元数据值
    /// </summary>
    public sealed record MetaList(IReadOnlyList<MetaValue> values) : MetaValue;

    /// <summary>
    ///     映射元数据值
    /// </summary>
    public sealed record MetaMap(IReadOnlyDictionary<string, MetaValue> values) : MetaValue;
}