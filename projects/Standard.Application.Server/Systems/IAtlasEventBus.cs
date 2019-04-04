namespace Std.App.Server.Systems;

/// <summary>
///     Atlas 事件总线接口，提供系统间发布/订阅式通信。
/// </summary>
public interface IAtlasEventBus
{
    /// <summary>
    ///     异步发布事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件对象</param>
    Task PublishAsync<T>(T @event) where T : class;
}