namespace Std.Net.WebSocket;

/// <summary>
///     WebSocket 关闭状态码（RFC 6455 第 7.4 节）。
/// </summary>
public enum WebSocketCloseStatus : ushort
{
    /// <summary>正常关闭</summary>
    normal_closure = 1000,

    /// <summary>端点离开</summary>
    going_away = 1001,

    /// <summary>协议错误</summary>
    protocol_error = 1002,

    /// <summary>不支持的数据类型</summary>
    unsupported_data = 1003,

    /// <summary>保留</summary>
    reserved = 1004,

    /// <summary>未收到状态码</summary>
    no_status_received = 1005,

    /// <summary>异常关闭</summary>
    abnormal_closure = 1006,

    /// <summary>无效的载荷数据</summary>
    invalid_payload_data = 1007,

    /// <summary>策略违规</summary>
    policy_violation = 1008,

    /// <summary>消息过大</summary>
    message_too_big = 1009,

    /// <summary>缺少必需的扩展</summary>
    mandatory_extension = 1010,

    /// <summary>内部服务器错误</summary>
    internal_server_error = 1011
}