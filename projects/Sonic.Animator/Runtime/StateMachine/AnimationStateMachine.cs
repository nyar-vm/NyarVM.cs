using System.Collections.Generic;

namespace Animator.Runtime.StateMachine;

/// <summary>
///     动画状态机
/// </summary>
public sealed class AnimationStateMachine
{
    private readonly Dictionary<string, AnimationState> _states = new();

    /// <summary>当前状态名称</summary>
    public string current_state_name { get; private set; } = string.Empty;

    /// <summary>添加动画状态</summary>
    public void add_state(string stateName, AnimationState state)
    {
        _states[stateName] = state;
    }

    /// <summary>尝试切换到指定状态</summary>
    public bool try_transition(string targetStateName)
    {
        if (!_states.TryGetValue(targetStateName, out var targetState)) return false;

        if (!string.IsNullOrEmpty(current_state_name) && _states.TryGetValue(current_state_name, out var currentState))
            if (!currentState.can_transition_to(targetStateName))
                return false;

        current_state_name = targetStateName;
        return true;
    }

    /// <summary>获取当前状态</summary>
    public AnimationState? get_current_state()
    {
        if (string.IsNullOrEmpty(current_state_name)) return null;

        return _states.TryGetValue(current_state_name, out var state) ? state : null;
    }
}

/// <summary>
///     动画状态
/// </summary>
public sealed class AnimationState
{
    private readonly HashSet<string> _transitions = [];

    public AnimationState(string stateName, string animationName, bool loopExecution)
    {
        state_name = stateName;
        animation_name = animationName;
        loop_execution = loopExecution;
    }

    /// <summary>状态名称</summary>
    public string state_name { get; }

    /// <summary>动画名称</summary>
    public string animation_name { get; }

    /// <summary>是否循环播放</summary>
    public bool loop_execution { get; }

    /// <summary>添加可转移的目标状态</summary>
    public void add_transition(string targetStateName)
    {
        _transitions.Add(targetStateName);
    }

    /// <summary>判断是否可以转移到目标状态</summary>
    public bool can_transition_to(string targetStateName)
    {
        return _transitions.Count == 0 || _transitions.Contains(targetStateName);
    }
}