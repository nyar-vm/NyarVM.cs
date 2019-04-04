using System;

namespace Core.Data;

/// <summary>
///     标记持久化实体，默认仅生成存储映射器。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class EntityAttribute : Attribute
{
    /// <summary>
    ///     是否生成序列化器。
    /// </summary>
    public bool generate_serializer { get; set; } = false;

    /// <summary>
    ///     是否生成验证器。
    /// </summary>
    public bool generate_validator { get; set; } = false;

    /// <summary>
    ///     是否生成存储映射器。
    /// </summary>
    public bool generate_storage_mapper { get; set; } = true;
}