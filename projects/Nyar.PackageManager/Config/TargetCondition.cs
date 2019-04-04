using Core.Data;

namespace Nyar.PackageManager.Config;

/// <summary>
///     目标平台条件依赖
/// </summary>
[Data]
public class TargetCondition
{
    /// <summary>
    ///     目标架构（如 <c>"wasm"</c>、<c>"jvm"</c>、<c>"clr"</c>）
    /// </summary>
    public string arch { get; set; } = string.Empty;

    /// <summary>
    ///     目标操作系统（如 <c>"web"</c>、<c>"linux"</c>、<c>"windows"</c>）
    /// </summary>
    public string? os { get; set; }

    /// <summary>
    ///     目标 ABI（如 <c>"wasip1"</c>）
    /// </summary>
    public string? abi { get; set; }

    /// <summary>
    ///     渠道（如 <c>"steam"</c>、<c>"wechat"</c>）
    /// </summary>
    public string? channel { get; set; }
}
