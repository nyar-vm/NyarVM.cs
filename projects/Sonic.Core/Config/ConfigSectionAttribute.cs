using System;

namespace Core.Config;

/// <summary>
///     配置节特性，标记类为配置节并指定节名称和优先级
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigSectionAttribute : Attribute
{
    /// <summary>
    ///     获取或设置配置节名称
    /// </summary>
    public string? section { get; set; }

    /// <summary>
    ///     获取或设置配置节优先级，默认为 0
    /// </summary>
    public int priority { get; set; } = 0;
}