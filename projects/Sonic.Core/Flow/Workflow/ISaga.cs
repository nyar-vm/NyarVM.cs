namespace Core.Flow.Workflow;

/// <summary>
///     长事务 saga 接口，提供状态持久化能力
/// </summary>
/// <typeparam name="T">saga 状态类型</typeparam>
public interface ISaga<T>
{
    /// <summary>
    ///     当前 saga 状态
    /// </summary>
    T state { get; }

    /// <summary>
    ///     持久化当前 saga 状态
    /// </summary>
    void persist();
}