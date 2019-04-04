using System;

namespace Core.Net.Rpc;

/// <summary>
///     标记方法为 RPC 方法，可指定流类型
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RpcMethodAttribute : Attribute
{
    /// <summary>
    ///     RPC 流类型，默认为单向调用
    /// </summary>
    public RpcStreamType stream_type { get; set; } = RpcStreamType.unary;
}