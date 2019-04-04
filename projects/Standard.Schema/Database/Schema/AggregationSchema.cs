namespace Hermes.Database.Schema;

/// <summary>
///     后端定义——描述数据存储后端的配置
/// </summary>
public sealed class BackendDefinition
{
    /// <summary>
    ///     初始化 <see cref="BackendDefinition" /> 类的新实例
    /// </summary>
    public BackendDefinition(string name, StorageBackendKind kind, IReadOnlyDictionary<string, string>? config = null)
    {
        Name = name;
        Kind = kind;
        Config = config ?? new Dictionary<string, string>();
    }

    /// <summary>
    ///     后端名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     后端类型
    /// </summary>
    public StorageBackendKind Kind { get; }

    /// <summary>
    ///     后端配置
    /// </summary>
    public IReadOnlyDictionary<string, string> Config { get; }
}

/// <summary>
///     聚合映射——描述类型在不同后端之间的映射关系
/// </summary>
public sealed class AggregationMapping
{
    /// <summary>
    ///     初始化 <see cref="AggregationMapping" /> 类的新实例
    /// </summary>
    public AggregationMapping(SchemaType sourceType, string sourceBackend, string targetBackend,
        IReadOnlyList<FieldMapping> fieldMappings)
    {
        SourceType = sourceType;
        SourceBackend = sourceBackend;
        TargetBackend = targetBackend;
        FieldMappings = fieldMappings;
    }

    /// <summary>
    ///     源类型
    /// </summary>
    public SchemaType SourceType { get; }

    /// <summary>
    ///     源后端名称
    /// </summary>
    public string SourceBackend { get; }

    /// <summary>
    ///     目标后端名称
    /// </summary>
    public string TargetBackend { get; }

    /// <summary>
    ///     字段映射列表
    /// </summary>
    public IReadOnlyList<FieldMapping> FieldMappings { get; }
}