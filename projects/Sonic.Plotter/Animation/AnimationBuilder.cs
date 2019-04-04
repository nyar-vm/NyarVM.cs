using Plotter.Core;

namespace Plotter.Animation;

/// <summary>
///     动画构建器，用于配置图表的动画效果。
/// </summary>
public sealed class AnimationBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="AnimationBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public AnimationBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     是否启用动画。
    /// </summary>
    internal bool _enabled { get; private set; }

    /// <summary>
    ///     动画持续时间（毫秒）。
    /// </summary>
    internal int _duration { get; private set; } = 300;

    /// <summary>
    ///     缓动风格。
    /// </summary>
    internal EasingStyle _easing_style { get; private set; } = EasingStyle.EaseInOut;

    /// <summary>
    ///     入场动画类型。
    /// </summary>
    internal string _enter_animation { get; private set; } = "";

    /// <summary>
    ///     更新动画类型。
    /// </summary>
    internal string _update_animation { get; private set; } = "";

    /// <summary>
    ///     退场动画类型。
    /// </summary>
    internal string _exit_animation { get; private set; } = "";

    /// <summary>
    ///     设置是否启用动画。
    /// </summary>
    /// <param name="enabled">是否启用。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnimationBuilder with_enabled(bool enabled)
    {
        _enabled = enabled;
        return this;
    }

    /// <summary>
    ///     设置动画持续时间。
    /// </summary>
    /// <param name="duration_ms">持续时间（毫秒）。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnimationBuilder with_duration(int duration_ms)
    {
        _duration = duration_ms;
        return this;
    }

    /// <summary>
    ///     设置缓动风格。
    /// </summary>
    /// <param name="style">缓动风格。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnimationBuilder with_easing_style(EasingStyle style)
    {
        _easing_style = style;
        return this;
    }

    /// <summary>
    ///     设置入场动画类型。
    /// </summary>
    /// <param name="animation_type">动画类型名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnimationBuilder with_enter_animation(string animation_type)
    {
        _enter_animation = animation_type;
        return this;
    }

    /// <summary>
    ///     设置更新动画类型。
    /// </summary>
    /// <param name="animation_type">动画类型名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnimationBuilder with_update_animation(string animation_type)
    {
        _update_animation = animation_type;
        return this;
    }

    /// <summary>
    ///     设置退场动画类型。
    /// </summary>
    /// <param name="animation_type">动画类型名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnimationBuilder with_exit_animation(string animation_type)
    {
        _exit_animation = animation_type;
        return this;
    }

    /// <summary>
    ///     完成动画配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}