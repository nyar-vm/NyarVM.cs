namespace Std.App.Server.Systems;

/// <summary>
///     Atlas 事件处理器接口，用于订阅事件总线上的事件。
/// </summary>
/// <typeparam name="T">事件类型</typeparam>
public interface IEventHandler<T> where T : class
{
    /// <summary>
    ///     异步处理事件
    /// </summary>
    /// <param name="event">事件对象</param>
    Task HandleAsync(T @event);
}