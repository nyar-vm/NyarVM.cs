namespace Nyar.Dialect.Schema.IR.Where;

/// <summary>
///     RPC 端点的流式通信模式
/// </summary>
public enum StreamingMode
{
    /// <summary>
    ///     一元调用：单个请求 → 单个响应
    /// </summary>
    unary = 0,

    /// <summary>
    ///     服务端流：单个请求 → 流式响应（IAsyncEnumerable）
    /// </summary>
    server = 1,

    /// <summary>
    ///     客户端流：流式请求（IAsyncEnumerable）→ 单个响应
    /// </summary>
    client = 2,

    /// <summary>
    ///     双向流：流式请求（IAsyncEnumerable）↔ 流式响应（IAsyncEnumerable）
    /// </summary>
    duplex = 3
}