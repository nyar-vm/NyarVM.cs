using System.Threading.Tasks;

namespace Core.Flow.Message;

/// <summary>
///     事件存储接口
/// </summary>
public interface IEventStore
{
    /// <summary>
    ///     异步追加事件到指定流
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="stream">流名称</param>
    /// <param name="event">事件实例</param>
    Task append<T>(string stream, T @event);
}