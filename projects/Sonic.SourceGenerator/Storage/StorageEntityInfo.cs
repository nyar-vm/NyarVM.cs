namespace Sonic.Data.Generator.Storage;

/// <summary>
///     存储实体的属性信息，用于源代码生成。
/// </summary>
internal readonly struct StoragePropertyInfo
{
    /// <summary>
    ///     属性名称。
    /// </summary>
    public readonly string name;

    /// <summary>
    ///     属性类型完全限定名称。
    /// </summary>
    public readonly string Type;

    /// <summary>
    ///     初始化 <see cref="StoragePropertyInfo" /> 的新实例。
    /// </summary>
    public StoragePropertyInfo(string name, string type)
    {
        this.name = name;
        Type = type;
    }
}

/// <summary>
///     存储实体的元数据信息，用于源代码生成。
/// </summary>
internal readonly struct StorageEntityInfo
{
    /// <summary>
    ///     实体类型名称。
    /// </summary>
    public readonly string type_name;

    /// <summary>
    ///     实体类型完全限定名称。
    /// </summary>
    public readonly string fully_qualified_name;

    /// <summary>
    ///     命名空间名称。
    /// </summary>
    public readonly string? namespace_name;

    /// <summary>
    ///     主键属性信息。
    /// </summary>
    public readonly StoragePropertyInfo key_property;

    /// <summary>
    ///     索引属性列表。
    /// </summary>
    public readonly List<StoragePropertyInfo> index_properties;

    /// <summary>
    ///     初始化 <see cref="StorageEntityInfo" /> 的新实例。
    /// </summary>
    public StorageEntityInfo(
        string typeName,
        string fullyQualifiedName,
        string? namespaceName,
        StoragePropertyInfo keyProperty,
        List<StoragePropertyInfo> indexProperties)
    {
        type_name = typeName;
        fully_qualified_name = fullyQualifiedName;
        namespace_name = namespaceName;
        key_property = keyProperty;
        index_properties = indexProperties;
    }
}