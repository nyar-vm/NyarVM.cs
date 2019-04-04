namespace Std.Flow.Workflow;

/// <summary>
///     长事务 Saga 接口，提供工作流编排和状态持久化能力。
///     源代码生成器会为标记了 <c>[Workflow]</c> 的类自动实现此接口。
/// </summary>
/// <typeparam name="T">Saga 状态类型。</typeparam>
public interface ISaga<T>
{
    /// <summary>
    ///     当前 Saga 状态。
    /// </summary>
    T state { get; }

    /// <summary>
    ///     持久化当前 Saga 状态。
    /// </summary>
    void persist();

    /// <summary>
    ///     按依赖顺序执行工作流的所有步骤。
    /// </summary>
    /// <param name="context">工作流上下文。</param>
    Task execute(T context);

    /// <summary>
    ///     按逆序执行已完成步骤的补偿操作。
    /// </summary>
    /// <param name="context">工作流上下文。</param>
    Task compensate(T context);
}