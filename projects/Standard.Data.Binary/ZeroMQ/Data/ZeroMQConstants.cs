namespace Std.Data.Binary.ZeroMQ.Data;

/// <summary>
///     ZeroMQ 协议常量的
/// </summary>
public static class ZeroMqConstants
{
    /// <summary>
    ///     命令类型的
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        ///     连接命令的
        /// </summary>
        connect = 1,

        /// <summary>
        ///     绑定命令的
        /// </summary>
        bind = 2,

        /// <summary>
        ///     轮询命令的
        /// </summary>
        poll = 3,

        /// <summary>
        ///     发送命令的
        /// </summary>
        send = 4,

        /// <summary>
        ///     接收命令的
        /// </summary>
        recv = 5,

        /// <summary>
        ///     关闭命令的
        /// </summary>
        close = 6,

        /// <summary>
        ///     终止命令的
        /// </summary>
        terminate = 7
    }

    /// <summary>
    ///     标志的
    /// </summary>
    [Flags]
    public enum Flags
    {
        /// <summary>
        ///     无标志的
        /// </summary>
        none = 0,

        /// <summary>
        ///     更多消息部分的
        /// </summary>
        more = 1,

        /// <summary>
        ///     不要等待的
        /// </summary>
        dont_wait = 2,

        /// <summary>
        ///     发送高水印的
        /// </summary>
        snd_hwm = 4,

        /// <summary>
        ///     接收高水印的
        /// </summary>
        rcv_hwm = 8,

        /// <summary>
        ///     发送超时的
        /// </summary>
        snd_timeout = 16,

        /// <summary>
        ///     接收超时的
        /// </summary>
        rcv_timeout = 32,

        /// <summary>
        ///     linger的
        /// </summary>
        linger = 64,

        /// <summary>
        ///     重连间隔的
        /// </summary>
        reconnect_ivl = 128,

        /// <summary>
        ///     最大重连间隔的
        /// </summary>
        reconnect_ivl_max = 256,

        /// <summary>
        ///     退避的
        /// </summary>
        backlog = 512,

        /// <summary>
        ///     ipv4 只的
        /// </summary>
        ipv4_only = 1024,

        /// <summary>
        ///     延迟连接的
        /// </summary>
        delay_attach_on_connect = 2048,

        /// <summary>
        ///     接受连接的
        /// </summary>
        accept_conn = 4096,

        /// <summary>
        ///     最大消息大小的
        /// </summary>
        max_msg_size = 8192,

        /// <summary>
        ///     多播循环的
        /// </summary>
        multicast_loop = 16384,

        /// <summary>
        ///     路由 ID的
        /// </summary>
        router_mandatory = 32768,

        /// <summary>
        ///     路由 ID的
        /// </summary>
        router_handover = 65536,

        /// <summary>
        ///     路由 ID的
        /// </summary>
        router_raw = 131072,

        /// <summary>
        ///     订阅的
        /// </summary>
        subscribe = 262144,

        /// <summary>
        ///     取消订阅的
        /// </summary>
        unsubscribe = 524288
    }

    /// <summary>
    ///     消息类型的
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///     消息部分的
        /// </summary>
        message_part = 0,

        /// <summary>
        ///     消息结束的
        /// </summary>
        message_end = 1
    }

    /// <summary>
    ///     套接字类型的
    /// </summary>
    public enum SocketType
    {
        /// <summary>
        ///     对一对一的
        /// </summary>
        pair = 0,

        /// <summary>
        ///     发布-订阅的
        /// </summary>
        pub = 1,

        /// <summary>
        ///     订阅-发布的
        /// </summary>
        sub = 2,

        /// <summary>
        ///     请求-响应的
        /// </summary>
        req = 3,

        /// <summary>
        ///     响应-请求的
        /// </summary>
        rep = 4,

        /// <summary>
        ///     推的拉取的
        /// </summary>
        dealer = 5,

        /// <summary>
        ///     拉取-推送的
        /// </summary>
        router = 6,

        /// <summary>
        ///     推送的
        /// </summary>
        push = 7,

        /// <summary>
        ///     拉取的
        /// </summary>
        pull = 8
    }

    /// <summary>
    ///     ZeroMQ 版本的
    /// </summary>
    public const int version = 3;

    /// <summary>
    ///     帧大小的
    /// </summary>
    public const int frame_size = 8;
}