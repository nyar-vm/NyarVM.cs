using System;

namespace Core.Marker;

/// <summary>
///     标记尚未稳定的 API，使用时会触发编译警告。
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class SonicExperimentalAttribute : Attribute
{
    /// <summary>
    ///     获取或设置功能名称。
    /// </summary>
    public string? feature { get; init; }

    /// <summary>
    ///     获取或设置引入版本。
    /// </summary>
    public string? since { get; init; }

    /// <summary>
    ///     获取或设置不稳定原因。
    /// </summary>
    public string? reason { get; init; }
}