using System;

namespace Core.Net.Rpc;

/// <summary>
///     标记接口为 RPC 服务，可指定服务名称
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class RpcServiceAttribute : Attribute
{
    /// <summary>
    ///     服务名称，为 null 时使用接口名
    /// </summary>
    public string? service_name { get; set; }
}