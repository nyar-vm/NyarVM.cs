using Nyar.Dialect.Schema.IR.What;

namespace Hermes.CLI;

/// <summary>
///     Schema 差异比较结果
/// </summary>
public sealed class SchemaDiffResult
{
    /// <summary>新增的类</summary>
    public List<ClassDefinition> added_classes { get; } = [];

    /// <summary>删除的类</summary>
    public List<ClassDefinition> deleted_classes { get; } = [];

    /// <summary>修改的类</summary>
    public List<(ClassDefinition, ClassDefinition, FieldDiffResult)> modified_classes { get; } = [];

    /// <summary>新增的枚举</summary>
    public List<EnumDefinition> added_enums { get; } = [];

    /// <summary>删除的枚举</summary>
    public List<EnumDefinition> deleted_enums { get; } = [];

    /// <summary>修改的枚举</summary>
    public List<(EnumDefinition, EnumDefinition)> modified_enums { get; } = [];

    /// <summary>新增的标志</summary>
    public List<FlagsDefinition> added_flags { get; } = [];

    /// <summary>删除的标志</summary>
    public List<FlagsDefinition> deleted_flags { get; } = [];

    /// <summary>修改的标志</summary>
    public List<(FlagsDefinition, FlagsDefinition)> modified_flags { get; } = [];

    /// <summary>新增的联合体</summary>
    public List<UnionDefinition> added_unions { get; } = [];

    /// <summary>删除的联合体</summary>
    public List<UnionDefinition> deleted_unions { get; } = [];

    /// <summary>修改的联合体</summary>
    public List<(UnionDefinition, UnionDefinition)> modified_unions { get; } = [];

    /// <summary>新增的服务</summary>
    public List<ServiceDefinition> added_services { get; } = [];

    /// <summary>删除的服务</summary>
    public List<ServiceDefinition> deleted_services { get; } = [];

    /// <summary>修改的服务</summary>
    public List<(ServiceDefinition, ServiceDefinition)> modified_services { get; } = [];

    /// <summary>新增的模型</summary>
    public List<ModelDefinition> added_models { get; } = [];

    /// <summary>删除的模型</summary>
    public List<ModelDefinition> deleted_models { get; } = [];

    /// <summary>是否存在变更</summary>
    public bool has_changes => added_classes.Count > 0 || deleted_classes.Count > 0 || modified_classes.Count > 0
                               || added_enums.Count > 0 || deleted_enums.Count > 0 || modified_enums.Count > 0
                               || added_flags.Count > 0 || deleted_flags.Count > 0 || modified_flags.Count > 0
                               || added_unions.Count > 0 || deleted_unions.Count > 0 || modified_unions.Count > 0
                               || added_services.Count > 0 || deleted_services.Count > 0 || modified_services.Count > 0
                               || added_models.Count > 0 || deleted_models.Count > 0;
}