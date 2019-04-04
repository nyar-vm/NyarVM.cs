namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 序列
/// </summary>
public sealed class YamlSequence : YamlValue
{
    private readonly YamlValue[] _items;

    public YamlSequence(YamlValue[] items)
    {
        _items = items;
    }

    public override YamlValueType ValueType => YamlValueType.sequence;


    /// <summary>
    ///     元素数量
    /// </summary>
    public int count => _items.Length;


    /// <summary>
    ///     按索引获取元素
    /// </summary>
    public YamlValue this[int index] => _items[index];


    /// <summary>
    ///     获取所有元素
    /// </summary>
    public IReadOnlyList<YamlValue> items => _items;
}