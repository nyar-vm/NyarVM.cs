using Hermes.YYDB.Query;

namespace Hermes.Database.Schema;

/// <summary>
///     字段映射——描述源字段到目标字段的映射关系
/// </summary>
public sealed class FieldMapping
{
    /// <summary>
    ///     初始化 <see cref="FieldMapping" /> 类的新实例
    /// </summary>
    public FieldMapping(string sourceField, string targetField,
        FieldTransformKind transform = FieldTransformKind.Identity)
    {
        SourceField = sourceField;
        TargetField = targetField;
        Transform = transform;
    }

    /// <summary>
    ///     源字段名
    /// </summary>
    public string SourceField { get; }

    /// <summary>
    ///     目标字段名
    /// </summary>
    public string TargetField { get; }

    /// <summary>
    ///     字段变换类型
    /// </summary>
    public FieldTransformKind Transform { get; }
}

/// <summary>
///     字段变换类型
/// </summary>
public enum FieldTransformKind
{
    /// <summary>
    ///     不变换
    /// </summary>
    Identity,

    /// <summary>
    ///     哈希变换
    /// </summary>
    Hash,

    /// <summary>
    ///     加密变换
    /// </summary>
    Encrypt,

    /// <summary>
    ///     压缩变换
    /// </summary>
    Compress,

    /// <summary>
    ///     JSON 序列化
    /// </summary>
    JsonSerialize,

    /// <summary>
    ///     JSON 反序列化
    /// </summary>
    JsonDeserialize,

    /// <summary>
    ///     Base64 编码
    /// </summary>
    Base64Encode,

    /// <summary>
    ///     Base64 解码
    /// </summary>
    Base64Decode
}

/// <summary>
///     聚合计划——描述多后端数据聚合的执行计划
/// </summary>
public sealed class AggregationPlan
{
    /// <summary>
    ///     初始化 <see cref="AggregationPlan" /> 类的新实例
    /// </summary>
    public AggregationPlan(string name, IReadOnlyList<AggregationStep> steps,
        AggregationStrategy strategy = AggregationStrategy.Lazy)
    {
        Name = name;
        Steps = steps;
        Strategy = strategy;
    }

    /// <summary>
    ///     计划名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     聚合步骤列表
    /// </summary>
    public IReadOnlyList<AggregationStep> Steps { get; }

    /// <summary>
    ///     聚合策略
    /// </summary>
    public AggregationStrategy Strategy { get; }
}

/// <summary>
///     聚合步骤——描述单次后端间的数据迁移步骤
/// </summary>
public sealed class AggregationStep
{
    /// <summary>
    ///     初始化 <see cref="AggregationStep" /> 类的新实例
    /// </summary>
    public AggregationStep(string sourceBackend, string targetBackend, IReadOnlyList<FieldMapping> mappings,
        QueryExpression? query = null)
    {
        SourceBackend = sourceBackend;
        Query = query;
        TargetBackend = targetBackend;
        Mappings = mappings;
    }

    /// <summary>
    ///     源后端名称
    /// </summary>
    public string SourceBackend { get; }

    /// <summary>
    ///     查询条件
    /// </summary>
    public QueryExpression? Query { get; }

    /// <summary>
    ///     目标后端名称
    /// </summary>
    public string TargetBackend { get; }

    /// <summary>
    ///     字段映射列表
    /// </summary>
    public IReadOnlyList<FieldMapping> Mappings { get; }
}

/// <summary>
///     聚合策略
/// </summary>
public enum AggregationStrategy
{
    /// <summary>
    ///     立即执行
    /// </summary>
    Eager,

    /// <summary>
    ///     延迟执行
    /// </summary>
    Lazy,

    /// <summary>
    ///     最终一致
    /// </summary>
    Eventual,

    /// <summary>
    ///     按需执行
    /// </summary>
    OnDemand
}