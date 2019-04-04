using Std.Data;
using Std.DataProcess.Decode;

namespace Std.DataProcess.Encode;

/// <summary>
///     编码层异常，当数据格式非法（�?VarInt 编码超过 10 个字节）时抛出�?/// 数据不足不抛异常，通过 <see cref="Decoded{T}.bytes_consumed" /> 返回 0 表示�?///
/// </summary>
public class EncodeException : DataException
{
    /// <summary>
    ///     初始�?<see cref="EncodeException" /> 的新实例�?    ///
    /// </summary>
    public EncodeException()
    {
    }

    /// <summary>
    ///     使用指定错误消息初始�?<see cref="EncodeException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">描述错误的消息�?/param>
    public EncodeException(string message) : base(message)
    {
    }

    /// <summary>
    ///     使用指定错误消息和内部异常初始化 <see cref="EncodeException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">
    ///     描述错误的消息�?/param>
    ///     <param name="innerException">导致当前异常的内部异常�?/param>
    public EncodeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}