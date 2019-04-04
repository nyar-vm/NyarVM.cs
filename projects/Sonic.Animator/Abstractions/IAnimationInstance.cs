using Animator.Core;
using Plotter.Rendering;

namespace Animator.Abstractions;

/// <summary>
///     动画运行实例统一契约
/// </summary>
public interface IAnimationInstance
{
    #region 只读状态属性

    /// <summary>当前播放状态</summary>
    PlayStatus current_play_status { get; }

    /// <summary>水平位置</summary>
    float horizontal_position { get; }

    /// <summary>垂直位置</summary>
    float vertical_position { get; }

    /// <summary>缩放比例</summary>
    float scale_ratio { get; }

    /// <summary>不透明度</summary>
    float opacity_value { get; }

    /// <summary>播放速度</summary>
    float playback_speed { get; }

    #endregion

    #region 配置类方法（动宾结构、链式返回）

    /// <summary>移动到指定位置</summary>
    IAnimationInstance move_to(float horizontal, float vertical);

    /// <summary>按指定比例缩放</summary>
    IAnimationInstance scale_by(float ratio);

    /// <summary>应用不透明度</summary>
    IAnimationInstance apply_opacity(float value);

    /// <summary>调整播放速度</summary>
    IAnimationInstance adjust_playback_speed(float speed);

    #endregion

    #region 行为类方法（纯动词、动作指令）

    /// <summary>播放指定动画</summary>
    IAnimationInstance play(string animationName, bool loopExecution = true);

    /// <summary>暂停播放</summary>
    IAnimationInstance pause();

    /// <summary>停止播放</summary>
    IAnimationInstance stop();

    #endregion

    #region 帧更新与渲染

    /// <summary>更新帧</summary>
    void update_frame(float deltaTime);

    /// <summary>绘制到画布</summary>
    void draw_to_canvas(IRenderCanvas canvas);

    #endregion
}