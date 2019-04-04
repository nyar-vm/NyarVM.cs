namespace Std.Config.Node;

/// <summary>
///     轻量级配置树节点的抽象基类，屏蔽不同数据格式的结构差异�?///
/// </summary>
public abstract class ConfigNode
{
    /// <summary>
    ///     配置节点的类型�?    ///
    /// </summary>
    public enum NodeType
    {
        /// <summary>
        ///     对象节点，包含命名字段�?        ///
        /// </summary>
        @object = 0,

        /// <summary>
        ///     数组节点，包含有序元素列表�?        ///
        /// </summary>
        array = 1,

        /// <summary>
        ///     标量节点，包含单个原始值�?        ///
        /// </summary>
        scalar = 2,

        /// <summary>
        ///     空节点，表示缺失或空值�?        ///
        /// </summary>
        @null = 3
    }

    /// <summary>
    ///     获取配置节点的类型�?    ///
    /// </summary>
    public abstract NodeType type { get; }

    /// <summary>
    ///     将节点值转换为字符串。非标量节点返回 null�?    ///
    /// </summary>
    /// <returns>字符串值，�?null�?/returns>
    public virtual string? as_string()
    {
        return null;
    }

    /// <summary>
    ///     将节点值转换为布尔值。非标量节点返回 false�?    ///
    /// </summary>
    /// <returns>布尔值�?/returns>
    public virtual bool as_boolean()
    {
        return false;
    }

    /// <summary>
    ///     将节点值转换为 32 位整数。非标量节点或转换失败返�?0�?    ///
    /// </summary>
    /// <returns>32 位整数值�?/returns>
    public virtual int as_int32()
    {
        return 0;
    }

    /// <summary>
    ///     将节点值转换为 64 位整数。非标量节点或转换失败返�?0�?    ///
    /// </summary>
    /// <returns>64 位整数值�?/returns>
    public virtual long as_int64()
    {
        return 0;
    }

    /// <summary>
    ///     将节点值转换为双精度浮点数。非标量节点或转换失败返�?0�?    ///
    /// </summary>
    /// <returns>双精度浮点数值�?/returns>
    public virtual double as_double()
    {
        return 0.0;
    }

    /// <summary>
    ///     将节点值转换为单精度浮点数。非标量节点或转换失败返�?0�?    ///
    /// </summary>
    /// <returns>单精度浮点数值�?/returns>
    public virtual float as_float()
    {
        return 0.0f;
    }

    /// <summary>
    ///     将节点值转换为十进制数。非标量节点或转换失败返�?0�?    ///
    /// </summary>
    /// <returns>十进制数值�?/returns>
    public virtual decimal as_decimal()
    {
        return 0m;
    }

    /// <summary>
    ///     获取对象节点中指定名称的字段。非对象节点返回 null�?    ///
    /// </summary>
    /// <param name="name">
    ///     字段名称�?/param>
    ///     <returns>字段对应的配置节点，�?null�?/returns>
    public virtual ConfigNode? get_field(string name)
    {
        return null;
    }

    /// <summary>
    ///     枚举对象节点的所有字段。非对象节点返回空序列�?    ///
    /// </summary>
    /// <returns>字段名与节点的键值对序列�?/returns>
    public virtual IEnumerable<KeyValuePair<string, ConfigNode>> enumerate_fields()
    {
        return [];
    }

    /// <summary>
    ///     枚举数组节点的所有元素。非数组节点返回空序列�?    ///
    /// </summary>
    /// <returns>数组元素序列�?/returns>
    public virtual IEnumerable<ConfigNode> enumerate_array()
    {
        return [];
    }
}