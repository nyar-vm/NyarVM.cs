using System.Threading.Tasks;

namespace Core.Concurrency;

/// <summary>
///     Actor 模型参与者接口，定义消息接收行为
/// </summary>
public interface IActor
{
    /// <summary>
    ///     异步接收并处理消息
    /// </summary>
    /// <param name="message">接收到的消息对象</param>
    Task receive(object message);
}