using System.Threading.Tasks;

namespace Core.Flow.Message;

/// <summary>
///     消息处理器接口
/// </summary>
/// <typeparam name="T">消息类型</typeparam>
public interface IMessageHandler<T>
{
    /// <summary>
    ///     异步处理消息
    /// </summary>
    /// <param name="message">消息实例</param>
    Task handle(T message);
}