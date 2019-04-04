using System;

namespace Core.Data;

/// <summary>
///     标记数据传输对象，控制序列化、反序列化、验证、架构和存储映射的自动生成。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DataAttribute : Attribute
{
    /// <summary>
    ///     是否生成序列化器。
    /// </summary>
    public bool generate_serializer { get; set; } = true;

    /// <summary>
    ///     是否生成反序列化器。
    /// </summary>
    public bool generate_deserializer { get; set; } = true;

    /// <summary>
    ///     是否生成验证器。
    /// </summary>
    public bool generate_validator { get; set; } = true;

    /// <summary>
    ///     是否生成架构定义。
    /// </summary>
    public bool generate_schema { get; set; } = true;

    /// <summary>
    ///     是否生成存储映射器。
    /// </summary>
    public bool generate_storage_mapper { get; set; } = false;

    /// <summary>
    ///     全局字段重命名策略，例如 "snake_case"、"camelCase"、"PascalCase"。
    /// </summary>
    public string? rename_all { get; set; }

    /// <summary>
    ///     数据版本号。
    /// </summary>
    public int version { get; set; } = 1;
}