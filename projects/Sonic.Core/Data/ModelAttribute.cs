using System;

namespace Core.Data;

/// <summary>
///     标记领域模型对象，控制序列化、验证、架构和存储映射的自动生成。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ModelAttribute : Attribute
{
    /// <summary>
    ///     是否生成序列化器。
    /// </summary>
    public bool generate_serializer { get; set; } = true;

    /// <summary>
    ///     是否生成验证器。
    /// </summary>
    public bool generate_validator { get; set; } = true;

    /// <summary>
    ///     是否生成架构定义。
    /// </summary>
    public bool generate_schema { get; set; } = false;

    /// <summary>
    ///     是否生成存储映射器。
    /// </summary>
    public bool generate_storage_mapper { get; set; } = true;

    /// <summary>
    ///     全局字段重命名策略，例如 "snake_case"、"camelCase"、"PascalCase"。
    /// </summary>
    public string? rename_all { get; set; }

    /// <summary>
    ///     模型版本号。
    /// </summary>
    public int version { get; set; } = 1;
}