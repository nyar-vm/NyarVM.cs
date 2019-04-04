using System;

namespace Core.Plugin;

/// <summary>
///     Plugin 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PluginAttribute : Attribute
{
    /// <summary>
    ///     插件名称
    /// </summary>
    public string? name { get; set; }


    /// <summary>
    ///     插件版本
    /// </summary>
    public string? version { get; set; }
}