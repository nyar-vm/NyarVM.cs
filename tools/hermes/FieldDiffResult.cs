using System.Reflection.Metadata;

namespace Hermes.CLI;

/// <summary>
///     字段差异比较结果
/// </summary>
public sealed class FieldDiffResult
{
    /// <summary>新增的字段</summary>
    public List<FieldDefinition> added { get; } = [];

    /// <summary>删除的字段</summary>
    public List<FieldDefinition> deleted { get; } = [];

    /// <summary>修改的字段</summary>
    public List<(FieldDefinition, FieldDefinition)> modified { get; } = [];

    /// <summary>是否存在变更</summary>
    public bool has_changes => added.Count > 0 || deleted.Count > 0 || modified.Count > 0;
}