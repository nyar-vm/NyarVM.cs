using Plotter.Animation;

namespace Plotter.Schema;

/// <summary>
///     动画配置，定义图表的动画效果参数。
/// </summary>
public class AnimationConfig
{
    /// <summary>
    ///     是否启用动画。
    /// </summary>
    public bool enabled { get; set; }

    /// <summary>
    ///     动画持续时间（毫秒）。
    /// </summary>
    public int duration { get; set; } = 300;

    /// <summary>
    ///     缓动风格。
    /// </summary>
    public EasingStyle easing_style { get; set; } = EasingStyle.EaseInOut;

    /// <summary>
    ///     是否启用入场动画。
    /// </summary>
    public bool enter_animation { get; set; }

    /// <summary>
    ///     是否启用更新动画。
    /// </summary>
    public bool update_animation { get; set; }

    /// <summary>
    ///     是否启用退场动画。
    /// </summary>
    public bool exit_animation { get; set; }
}