namespace Std.Config.Node;

/// <summary>
///     标量类型的配置节点，包含单个原始值（字符串、数字、布尔等）�?/// 内部统一以字符串形式存储，按需转换为各种目标类型�?///
/// </summary>
public sealed class ScalarConfigNode : ConfigNode
{
    private readonly string _value;

    /// <summary>
    ///     使用指定的字符串值初始化 <see cref="ScalarConfigNode" /> 的新实例�?    ///
    /// </summary>
    /// <param name="value">标量值的字符串表示�?/param>
    public ScalarConfigNode(string value)
    {
        _value = value ?? string.Empty;
    }

    /// <summary>
    ///     获取配置节点的类型，始终�?<see cref="ConfigNode.NodeType.Scalar" />�?    ///
    /// </summary>
    public override NodeType type => NodeType.scalar;

    /// <summary>
    ///     将节点值转换为字符串�?    ///
    /// </summary>
    /// <returns>字符串值�?/returns>
    public override string? as_string()
    {
        return _value;
    }

    /// <summary>
    ///     将节点值转换为布尔值�?    ///
    /// </summary>
    /// <returns>解析成功返回布尔值，失败返回 false�?/returns>
    public override bool as_boolean()
    {
        return bool.TryParse(_value, out var result) && result;
    }

    /// <summary>
    ///     将节点值转换为 32 位整数�?    ///
    /// </summary>
    /// <returns>解析成功返回整数值，失败返回 0�?/returns>
    public override int as_int32()
    {
        return int.TryParse(_value, out var result) ? result : 0;
    }

    /// <summary>
    ///     将节点值转换为 64 位整数�?    ///
    /// </summary>
    /// <returns>解析成功返回整数值，失败返回 0�?/returns>
    public override long as_int64()
    {
        return long.TryParse(_value, out var result) ? result : 0;
    }

    /// <summary>
    ///     将节点值转换为双精度浮点数�?    ///
    /// </summary>
    /// <returns>解析成功返回浮点数值，失败返回 0�?/returns>
    public override double as_double()
    {
        return double.TryParse(_value, out var result) ? result : 0.0;
    }

    /// <summary>
    ///     将节点值转换为单精度浮点数�?    ///
    /// </summary>
    /// <returns>解析成功返回浮点数值，失败返回 0�?/returns>
    public override float as_float()
    {
        return float.TryParse(_value, out var result) ? result : 0.0f;
    }

    /// <summary>
    ///     将节点值转换为十进制数�?    ///
    /// </summary>
    /// <returns>解析成功返回十进制数值，失败返回 0�?/returns>
    public override decimal as_decimal()
    {
        return decimal.TryParse(_value, out var result) ? result : 0m;
    }
}