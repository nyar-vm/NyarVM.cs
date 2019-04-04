namespace Core.Simulation.StateMachine;

/// <summary>
///     IStateMachine 接口
/// </summary>
public interface IStateMachine<T>
{
    /// <summary>
    ///     当前状态
    /// </summary>
    T current_state { get; }

    /// <summary>
    ///     尝试转换到目标状态
    /// </summary>
    bool try_transition(T target);
}