namespace Nyar.ObjectAlgebra;

/// <summary>
///     OperatorDescriptor 是 IOperatorDescriptor 的具体实现。
///     每个方言操作符对应一个实例，由 Source Generator 自动生成。
/// </summary>
public sealed class OperatorDescriptor : IOperatorDescriptor
{
    /// <summary>
    ///     初始化 OperatorDescriptor 实例
    /// </summary>
    /// <param name="key">操作符全局键。</param>
    /// <param name="name">操作符名称。</param>
    /// <param name="arity">子节点数量。</param>
    /// <param name="dialectName">所属方言名称。</param>
    /// <param name="payloadType">Payload 类型，可为 null。</param>
    public OperatorDescriptor(OperatorKey key, string name, int arity, string dialectName, Type? payloadType = null)
    {
        this.key = key;
        this.name = name;
        this.arity = arity;
        dialect_name = dialectName;
        payload_type = payloadType;
    }

    /// <inheritdoc />
    public OperatorKey key { get; }

    /// <inheritdoc />
    public string name { get; }

    /// <inheritdoc />
    public int arity { get; }

    /// <inheritdoc />
    public Type? payload_type { get; }

    /// <inheritdoc />
    public string dialect_name { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{dialect_name}.{name}({arity})";
    }
}