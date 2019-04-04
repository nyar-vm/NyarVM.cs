using System;
using System.Threading.Tasks;

namespace Core.Flow.Message;

/// <summary>
///     事件总线接口
/// </summary>
public interface IEventBus
{
    /// <summary>
    ///     异步发布事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    Task publish<T>(T @event);

    /// <summary>
    ///     订阅事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="handler">事件处理函数</param>
    /// <returns>可释放的订阅句柄</returns>
    IDisposable subscribe<T>(Func<T, Task> handler);
}