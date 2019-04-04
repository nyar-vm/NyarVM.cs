using Std.Data;

namespace Std.DataProcess.Serialize;

/// <summary>
///     序列化层异常，当格式不匹配、类型错误或结构异常时抛出�?/// 数据不足（如 <c>TryReadFieldName</c> 返回 false）不是错误，由调用者决定是否继续�?///
/// </summary>
public class SerializeException : DataException
{
    /// <summary>
    ///     初始�?<see cref="SerializeException" /> 的新实例�?    ///
    /// </summary>
    public SerializeException()
    {
    }

    /// <summary>
    ///     使用指定错误消息初始�?<see cref="SerializeException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">描述错误的消息�?/param>
    public SerializeException(string message) : base(message)
    {
    }

    /// <summary>
    ///     使用指定错误消息和内部异常初始化 <see cref="SerializeException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">
    ///     描述错误的消息�?/param>
    ///     <param name="innerException">导致当前异常的内部异常�?/param>
    public SerializeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}