namespace Oak.Wat;

/// <summary>
///     WAT 解析异常，携带源码位置信息
/// </summary>
public class WatParseException : Exception
{
    /// <summary>
    ///     使用错误消息初始化异常
    /// </summary>
    /// <param name="message">错误消息。</param>
    public WatParseException(string message) : base(message)
    {
    }


    /// <summary>
    ///     使用错误消息和源码位置初始化异常
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="line">错误行号。</param>
    /// <param name="column">错误列号。</param>
    public WatParseException(string message, int line, int column)
        : base($"第 {line} 行第 {column} 列: {message}")
    {
        Line = line;
        Column = column;
    }


    /// <summary>
    ///     使用错误消息和内部异常初始化异常
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="innerException">内部异常。</param>
    public WatParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     错误行号
    /// </summary>
    public int Line { get; }


    /// <summary>
    ///     错误列号
    /// </summary>
    public int Column { get; }
}