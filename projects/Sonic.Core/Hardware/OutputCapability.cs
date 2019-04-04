namespace Core.Hardware;

/// <summary>
///     输出能力枚举
/// </summary>
public enum OutputCapability
{
    /// <summary>
    ///     无输出能力
    /// </summary>
    none,

    /// <summary>
    ///     显示输出
    /// </summary>
    display,

    /// <summary>
    ///     扬声器输出
    /// </summary>
    speaker,

    /// <summary>
    ///     触觉反馈输出
    /// </summary>
    haptic,

    /// <summary>
    ///     打印输出
    /// </summary>
    printer
}