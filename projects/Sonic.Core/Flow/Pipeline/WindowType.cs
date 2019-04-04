namespace Core.Flow.Pipeline;

/// <summary>
///     WindowType 枚举
/// </summary>
public enum WindowType
{
    /// <summary>
    ///     滚动窗口
    /// </summary>
    tumbling,

    /// <summary>
    ///     滑动窗口
    /// </summary>
    sliding,

    /// <summary>
    ///     会话窗口
    /// </summary>
    session
}