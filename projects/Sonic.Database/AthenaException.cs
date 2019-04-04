namespace Olympus.Athena;

/// <summary>
///     Athena 数据库引擎内部异常
/// </summary>
public class AthenaException : Exception
{
    /// <summary>
    ///     创建 Athena 异常
    /// </summary>
    /// <param name="message">异常消息</param>
    public AthenaException(string message) : base(message)
    {
    }

    /// <summary>
    ///     创建 Athena 异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public AthenaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}