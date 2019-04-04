namespace Std.Data.Binary;

/// <summary>
///     表示协议中的一条消息
/// </summary>
public interface IMessage
{
    /// <summary>
    ///     消息类型标识
    /// </summary>
    int message_id { get; }
}

/// <summary>
///     表示协议会话状态
/// </summary>
public interface ISession
{
    /// <summary>
    ///     会话标识符
    /// </summary>
    string session_id { get; }

    /// <summary>
    ///     当前状态
    /// </summary>
    object state { get; set; }
}

/// <summary>
///     协议处理器，负责处理输入消息并产生输出
/// </summary>
public interface IProtocolHandler<TMessage> where TMessage : IMessage
{
    ValueTask handle(ISession session, TMessage message);
}