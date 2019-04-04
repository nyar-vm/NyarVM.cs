using Std.Data;

namespace Std.DataProcess.Enframe;

/// <summary>
///     分帧层异常，当帧格式错误（如 CRC 校验失败、非法长度前缀）时抛出�?/// 帧数据不足是正常流控状态，<c>TryGetNextFrame</c> 返回 false 而非抛异常�?///
/// </summary>
public class FramingException : DataException
{
    /// <summary>
    ///     初始�?<see cref="FramingException" /> 的新实例�?    ///
    /// </summary>
    public FramingException()
    {
    }

    /// <summary>
    ///     使用指定错误消息初始�?<see cref="FramingException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">描述错误的消息�?/param>
    public FramingException(string message) : base(message)
    {
    }

    /// <summary>
    ///     使用指定错误消息和内部异常初始化 <see cref="FramingException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">
    ///     描述错误的消息�?/param>
    ///     <param name="innerException">导致当前异常的内部异常�?/param>
    public FramingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}