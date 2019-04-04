namespace Core.Net.Rpc;

/// <summary>
///     RPC 流类型枚举
/// </summary>
public enum RpcStreamType
{
    /// <summary>
    ///     单向调用：客户端发送一个请求，服务端返回一个响应
    /// </summary>
    unary,

    /// <summary>
    ///     服务端流：客户端发送一个请求，服务端返回流式响应
    /// </summary>
    server_stream,

    /// <summary>
    ///     客户端流：客户端发送流式请求，服务端返回一个响应
    /// </summary>
    client_stream,

    /// <summary>
    ///     双向流：客户端和服务端均可发送流式数据
    /// </summary>
    duplex
}