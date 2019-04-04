namespace Core.Hardware;

/// <summary>
///     输入能力枚举
/// </summary>
public enum InputCapability
{
    /// <summary>
    ///     无输入能力
    /// </summary>
    none,

    /// <summary>
    ///     指针输入
    /// </summary>
    pointing,

    /// <summary>
    ///     按键输入
    /// </summary>
    keying,

    /// <summary>
    ///     运动输入
    /// </summary>
    motion,

    /// <summary>
    ///     触控输入
    /// </summary>
    touch,

    /// <summary>
    ///     语音输入
    /// </summary>
    voice,

    /// <summary>
    ///     眼动输入
    /// </summary>
    gaze
}