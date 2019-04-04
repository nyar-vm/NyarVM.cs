namespace Plotter.Animation;

/// <summary>
///     缓动风格枚举，定义动画的缓动函数类型。
/// </summary>
public enum EasingStyle
{
    /// <summary>
    ///     线性缓动。
    /// </summary>
    Linear,

    /// <summary>
    ///     缓入。
    /// </summary>
    EaseIn,

    /// <summary>
    ///     缓出。
    /// </summary>
    EaseOut,

    /// <summary>
    ///     缓入缓出。
    /// </summary>
    EaseInOut,

    /// <summary>
    ///     弹性缓动。
    /// </summary>
    Bounce
}