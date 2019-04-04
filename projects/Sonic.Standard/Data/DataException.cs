namespace Std.Data;

/// <summary>
///     Sonic.Standard.Data 所有异常的基类�?/// </供调用者统一捕获所有数据处理相关的错误�?/// </summary>
public class DataException : Exception
{
    /// <summary>
    ///     初始�?<see cref="DataException" /> 的新实例�?    ///
    /// </summary>
    public DataException()
    {
    }

    /// <summary>
    ///     使用指定错误消息初始�?<see cref="DataException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">描述错误的消息�?/param>
    public DataException(string message) : base(message)
    {
    }

    /// <summary>
    ///     使用指定错误消息和内部异常初始化 <see cref="DataException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">
    ///     描述错误的消息�?/param>
    ///     <param name="innerException">导致当前异常的内部异常�?/param>
    public DataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}