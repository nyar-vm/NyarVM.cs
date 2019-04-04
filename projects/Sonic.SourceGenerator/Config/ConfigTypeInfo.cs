using System.Collections.Immutable;

namespace Sonic.Data.Generator.Config;

/// <summary>
///     标记了 <c>[Config]</c> 特性的类型的编译期提取信息。
/// </summary>
internal readonly record struct ConfigTypeInfo
{
    /// <summary>
    ///     完全限定类型名，如 <c>global::MyApp.DatabaseConfig</c>。
    /// </summary>
    public readonly string fully_qualified_name;

    /// <summary>
    ///     命名空间名称，可能为 null（全局命名空间）。
    /// </summary>
    public readonly string? namespace_name;

    /// <summary>
    ///     属性到配置键名的命名约定。
    /// </summary>
    public readonly NamingConvention naming_convention;

    /// <summary>
    ///     该类型的所有可写配置属性列表。
    /// </summary>
    public readonly ImmutableArray<ConfigPropertyInfo> properties;

    /// <summary>
    ///     配置节名称，从 <c>[Config(SectionName)]</c> 获取，默认为类名 camelCase。
    /// </summary>
    public readonly string section_name;

    /// <summary>
    ///     类型名称（不含命名空间）。
    /// </summary>
    public readonly string type_name;

    /// <summary>
    ///     使用所有字段初始化 <see cref="ConfigTypeInfo" /> 的新实例。
    /// </summary>
    public ConfigTypeInfo(
        string type_name,
        string fully_qualified_name,
        string? namespace_name,
        string section_name,
        NamingConvention naming_convention,
        ImmutableArray<ConfigPropertyInfo> properties)
    {
        this.type_name = type_name;
        this.fully_qualified_name = fully_qualified_name;
        this.namespace_name = namespace_name;
        this.section_name = section_name;
        this.naming_convention = naming_convention;
        this.properties = properties;
    }
}