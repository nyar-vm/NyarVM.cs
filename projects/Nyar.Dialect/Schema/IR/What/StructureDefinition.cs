using Nyar.Dialect.Schema.IR.Common;

namespace Nyar.Dialect.Schema.IR.What;

/// <summary>
///     结构体定义——值类型的数据聚合（不可变、按值比较、无独立标识）
/// </summary>
public sealed class StructureDefinition
{
    /// <summary>
    ///     初始化 <see cref="StructureDefinition" /> 类的新实例
    /// </summary>
    public StructureDefinition(
        string name,
        IReadOnlyList<FieldDefinition> fields,
        IReadOnlyList<AttributeDefinition> attributes,
        string? sourceFile = null,
        int sourceLine = 0,
        int sourceColumn = 0)
    {
        this.name = name;
        this.fields = fields;
        this.attributes = attributes;
        source_file = sourceFile;
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    /// <summary>
    ///     结构体名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     字段列表
    /// </summary>
    public IReadOnlyList<FieldDefinition> fields { get; }

    /// <summary>
    ///     属性/注解列表
    /// </summary>
    public IReadOnlyList<AttributeDefinition> attributes { get; }

    /// <summary>
    ///     源文件路径
    /// </summary>
    public string? source_file { get; }

    /// <summary>
    ///     源码行号
    /// </summary>
    public int source_line { get; }

    /// <summary>
    ///     源码列号
    /// </summary>
    public int source_column { get; }
}