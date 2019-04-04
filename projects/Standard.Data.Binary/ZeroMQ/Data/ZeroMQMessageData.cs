namespace Std.Data.Binary.ZeroMQ.Data;

/// <summary>
///     ZeroMQ 消息数据结构的
/// </summary>
public class ZeroMqMessageData
{
    /// <summary>
    ///     获取或设置消息类型的
    /// </summary>
    public ZeroMqConstants.MessageType type { get; set; }

    /// <summary>
    ///     获取或设置消息长度的
    /// </summary>
    public int length { get; set; }

    /// <summary>
    ///     获取或设置消息内容的
    /// </summary>
    public byte[] data { get; set; }

    /// <summary>
    ///     获取或设置标志的
    /// </summary>
    public ZeroMqConstants.Flags flags { get; set; }

    /// <summary>
    ///     获取或设置命令类型（仅适用于命令消息）的
    /// </summary>
    public ZeroMqConstants.CommandType? command_type { get; set; }

    /// <summary>
    ///     获取或设置套接字类型（仅适用于连的绑定命令）的
    /// </summary>
    public ZeroMqConstants.SocketType? socket_type { get; set; }

    /// <summary>
    ///     获取或设置地址（仅适用于连的绑定命令）的
    /// </summary>
    public string address { get; set; }

    /// <summary>
    ///     获取或设置消息部分（仅适用于多部分消息）的
    /// </summary>
    public List<ZeroMqMessageData> parts { get; set; } = [];
}

/// <summary>
///     ZeroMQ 帧数据结构的
/// </summary>
public class ZeroMqFrameData
{
    /// <summary>
    ///     获取或设置帧标志的
    /// </summary>
    public byte flags { get; set; }

    /// <summary>
    ///     获取或设置帧长度的
    /// </summary>
    public int length { get; set; }

    /// <summary>
    ///     获取或设置帧内容的
    /// </summary>
    public byte[] data { get; set; }
}