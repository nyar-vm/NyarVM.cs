using System;

namespace Animator.Runtime.Timing;

/// <summary>
///     帧时间管理器（统一帧率、时间缩放）
/// </summary>
public sealed class FrameTimeManager
{
    private float _target_frame_interval;
    private float _time_scale = 1f;

    /// <summary>时间缩放因子</summary>
    public float time_scale
    {
        get => _time_scale;
        set => _time_scale = Math.Max(value, 0f);
    }

    /// <summary>目标帧率</summary>
    public float target_frame_rate
    {
        get => _target_frame_interval > 0f ? 1f / _target_frame_interval : 0f;
        set => _target_frame_interval = value > 0f ? 1f / value : 0f;
    }

    /// <summary>累计时间</summary>
    public float accumulated_time { get; private set; }

    /// <summary>
    ///     推进帧时间
    /// </summary>
    /// <param name="rawDeltaTime">原始帧间隔时间</param>
    /// <returns>缩放后的帧间隔时间</returns>
    public float advance(float rawDeltaTime)
    {
        var scaledDeltaTime = rawDeltaTime * _time_scale;
        accumulated_time += scaledDeltaTime;
        return scaledDeltaTime;
    }

    /// <summary>判断当前帧是否应该执行更新</summary>
    public bool should_update_frame()
    {
        return _target_frame_interval <= 0f || accumulated_time >= _target_frame_interval;
    }

    /// <summary>消耗一帧的累计时间</summary>
    public void consume_frame()
    {
        if (_target_frame_interval > 0f) accumulated_time -= _target_frame_interval;
    }

    /// <summary>重置累计时间</summary>
    public void reset()
    {
        accumulated_time = 0f;
    }
}