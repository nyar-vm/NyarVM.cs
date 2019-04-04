namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 映射
/// </summary>
public sealed class YamlMapping : YamlValue
{
    private readonly Dictionary<string, int> _index;
    private readonly (string Key, YamlValue Value)[] _properties;

    public YamlMapping((string Key, YamlValue Value)[] properties)
    {
        _properties = properties;
        _index = new Dictionary<string, int>(properties.Length);

        for (var i = 0; i < properties.Length; i++) _index.TryAdd(properties[i].Key, i);
    }

    public override YamlValueType ValueType => YamlValueType.mapping;


    /// <summary>
    ///     属性数量
    /// </summary>
    public int count => _properties.Length;


    /// <summary>
    ///     按键获取值
    /// </summary>
    public YamlValue? this[string key]
    {
        get
        {
            if (!_index.TryGetValue(key, out var i)) return null;

            return _properties[i].Value;
        }
    }


    /// <summary>
    ///     获取所有属性
    /// </summary>
    public IReadOnlyList<(string Key, YamlValue Value)> properties => _properties;


    /// <summary>
    ///     是否包含指定键
    /// </summary>
    public bool contains_key(string key)
    {
        return _index.ContainsKey(key);
    }


    /// <summary>
    ///     尝试获取值
    /// </summary>
    public bool try_get_value(string key, out YamlValue? value)
    {
        if (!_index.TryGetValue(key, out var i))
        {
            value = null;
            return false;
        }

        value = _properties[i].Value;
        return true;
    }
}