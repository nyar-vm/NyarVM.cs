namespace Nyar.ObjectAlgebra;

/// <summary>
///     操作符描述符，描述一个方言操作符的元数据。
///     每个方言中的操作符对应一个 IOperatorDescriptor 实例。
/// </summary>
public interface IOperatorDescriptor
{
    /// <summary>
    ///     操作符全局唯一键
    /// </summary>
    OperatorKey key { get; }

    /// <summary>
    ///     操作符名称（如 "add"、"mul"、"const_i64"）
    /// </summary>
    string name { get; }

    /// <summary>
    ///     操作符 arity（子节点数量）
    /// </summary>
    int arity { get; }

    /// <summary>
    ///     Payload 类型（如果操作符携带额外数据），否则为 null
    /// </summary>
    Type? payload_type { get; }

    /// <summary>
    ///     所属方言名称
    /// </summary>
    string dialect_name { get; }
}