using System;

namespace Animator.Runtime.StateMachine;

/// <summary>
///     动画过渡插值器
/// </summary>
public sealed class TransitionInterpolator
{
    private float _duration;
    private float _elapsed_time;

    /// <summary>是否正在过渡中</summary>
    public bool is_transitioning => _elapsed_time < _duration;

    /// <summary>当前过渡进度（0~1）</summary>
    public float progress => _duration > 0f ? Math.Min(_elapsed_time / _duration, 1f) : 1f;

    /// <summary>源动画名称</summary>
    public string source_animation_name { get; private set; } = string.Empty;

    /// <summary>目标动画名称</summary>
    public string target_animation_name { get; private set; } = string.Empty;

    /// <summary>开始过渡</summary>
    public void begin_transition(string sourceAnimationName, string targetAnimationName, float duration)
    {
        source_animation_name = sourceAnimationName;
        target_animation_name = targetAnimationName;
        _duration = duration;
        _elapsed_time = 0f;
    }

    /// <summary>推进过渡时间</summary>
    public void advance(float deltaTime)
    {
        if (is_transitioning) _elapsed_time += deltaTime;
    }

    /// <summary>立即完成过渡</summary>
    public void complete()
    {
        _elapsed_time = _duration;
    }
}