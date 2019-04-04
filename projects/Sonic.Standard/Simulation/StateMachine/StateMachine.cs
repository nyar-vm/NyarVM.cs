using Core.Simulation.StateMachine;

namespace Std.Simulation.StateMachine;

/// <summary>
///     状态机，实现 IStateMachine&lt;T&gt; 接口，管理状态转换
/// </summary>
/// <typeparam name="T">状态类型</typeparam>
public sealed class StateMachine<T> : IStateMachine<T> where T : notnull
{
    /// <summary>
    ///     允许的转换表
    /// </summary>
    private readonly Dictionary<T, HashSet<T>> _transitions = new();

    /// <summary>
    ///     初始化状态机
    /// </summary>
    /// <param name="initialState">初始状态</param>
    public StateMachine(T initialState)
    {
        current_state = initialState;
    }

    /// <summary>
    ///     当前状态
    /// </summary>
    public T current_state { get; private set; }

    /// <summary>
    ///     尝试转换到目标状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>是否转换成功</returns>
    public bool try_transition(T target)
    {
        if (_transitions.TryGetValue(current_state, out var targets) && targets.Contains(target))
        {
            current_state = target;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     添加允许的状态转换
    /// </summary>
    /// <param name="from">源状态</param>
    /// <param name="to">目标状态</param>
    public void add_transition(T from, T to)
    {
        if (!_transitions.TryGetValue(from, out var targets))
        {
            targets = [];
            _transitions[from] = targets;
        }

        targets.Add(to);
    }
}