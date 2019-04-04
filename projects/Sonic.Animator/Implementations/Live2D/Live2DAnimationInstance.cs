using Animator.Abstractions;
using Animator.Core;
using Plotter.Rendering;

namespace Animator.Implementations.Live2D;

/// <summary>
///     Live2D 动画运行实例
/// </summary>
internal sealed class Live2DAnimationInstance : IAnimationInstance
{
    private readonly IAnimationResource _inner_resource;
    private string _current_animation_name = string.Empty;
    private bool _loop_execution;

    public Live2DAnimationInstance(IAnimationResource resource)
    {
        _inner_resource = resource;
        current_play_status = PlayStatus.idle;
    }

    #region 只读状态属性

    /// <summary>当前播放状态</summary>
    public PlayStatus current_play_status { get; private set; }

    /// <summary>水平位置</summary>
    public float horizontal_position { get; private set; }

    /// <summary>垂直位置</summary>
    public float vertical_position { get; private set; }

    /// <summary>缩放比例</summary>
    public float scale_ratio { get; private set; } = 1.0f;

    /// <summary>不透明度</summary>
    public float opacity_value { get; private set; } = 1.0f;

    /// <summary>播放速度</summary>
    public float playback_speed { get; private set; } = 1.0f;

    #endregion

    #region 配置类方法（动宾结构、链式返回）

    /// <summary>移动到指定位置</summary>
    public IAnimationInstance move_to(float horizontal, float vertical)
    {
        horizontal_position = horizontal;
        vertical_position = vertical;
        return this;
    }

    /// <summary>按指定比例缩放</summary>
    public IAnimationInstance scale_by(float ratio)
    {
        scale_ratio = ratio;
        return this;
    }

    /// <summary>应用不透明度</summary>
    public IAnimationInstance apply_opacity(float value)
    {
        opacity_value = value;
        return this;
    }

    /// <summary>调整播放速度</summary>
    public IAnimationInstance adjust_playback_speed(float speed)
    {
        playback_speed = speed;
        return this;
    }

    #endregion

    #region 行为类方法（纯动词、动作指令）

    /// <summary>播放指定动画</summary>
    public IAnimationInstance play(string animationName, bool loopExecution = true)
    {
        _current_animation_name = animationName;
        _loop_execution = loopExecution;
        current_play_status = PlayStatus.playing;
        return this;
    }

    /// <summary>暂停播放</summary>
    public IAnimationInstance pause()
    {
        current_play_status = PlayStatus.paused;
        return this;
    }

    /// <summary>停止播放</summary>
    public IAnimationInstance stop()
    {
        current_play_status = PlayStatus.stopped;
        _current_animation_name = string.Empty;
        return this;
    }

    #endregion

    #region 帧更新与渲染

    /// <summary>更新帧</summary>
    public void update_frame(float deltaTime)
    {
        if (current_play_status != PlayStatus.playing) return;

        var scaledDeltaTime = deltaTime * playback_speed;
    }

    /// <summary>绘制到画布</summary>
    public void draw_to_canvas(IRenderCanvas canvas)
    {
        if (current_play_status == PlayStatus.stopped) return;

        var alpha = (byte)(opacity_value * 255f);
        canvas.set_fill_color(new PlotColor(255, 255, 255, alpha));
    }

    #endregion
}