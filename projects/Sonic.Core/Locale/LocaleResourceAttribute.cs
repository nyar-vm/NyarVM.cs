using System;

namespace Core.Locale;

/// <summary>
///     本地化资源特性，标记类为本地化资源并指定资源路径和默认区域
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LocaleResourceAttribute : Attribute
{
    /// <summary>
    ///     获取或设置资源路径
    /// </summary>
    public string? resource_path { get; set; }

    /// <summary>
    ///     获取或设置默认区域，默认为 "en"
    /// </summary>
    public string default_locale { get; set; } = "en";
}