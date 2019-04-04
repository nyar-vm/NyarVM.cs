using Std.Data;

namespace Std.DataProcess.Scan;

/// <summary>
///     扫描器异常，当路径语法错误或未找到指定字段时抛出�?///
/// </summary>
public class ScanException : DataException
{
    /// <summary>
    ///     初始�?<see cref="ScanException" /> 的新实例�?    ///
    /// </summary>
    public ScanException()
    {
    }

    /// <summary>
    ///     使用指定错误消息初始�?<see cref="ScanException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">描述错误的消息�?/param>
    public ScanException(string message) : base(message)
    {
    }

    /// <summary>
    ///     使用指定错误消息和内部异常初始化 <see cref="ScanException" /> 的新实例�?    ///
    /// </summary>
    /// <param name="message">
    ///     描述错误的消息�?/param>
    ///     <param name="innerException">导致当前异常的内部异常�?/param>
    public ScanException(string message, Exception innerException) : base(message, innerException)
    {
    }
}