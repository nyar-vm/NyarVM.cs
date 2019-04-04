namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 操作码，定义帧类型。
/// </summary>
public enum WebSocketOpcode : byte
{
    /// <summary>续帧</summary>
    continuation = 0x0,

    /// <summary>文本帧</summary>
    text = 0x1,

    /// <summary>二进制帧</summary>
    binary = 0x2,

    /// <summary>关闭帧</summary>
    close = 0x8,

    /// <summary>Ping 帧</summary>
    ping = 0x9,

    /// <summary>Pong 帧</summary>
    pong = 0xA
}