namespace Std.DL.Data;

/// <summary>
///     Galatea 标准数据集的逻辑 Schema，定义字段、分片和变换
/// </summary>
public sealed class DatasetSchema
{
    /// <summary>数据集名称</summary>
    public string Name { get; init; } = "";

    /// <summary>版本号</summary>
    public string Version { get; init; } = "0.1";

    /// <summary>数据来源</summary>
    public string? Source { get; init; }

    /// <summary>输入字段定义</summary>
    public IReadOnlyList<FieldDef> Inputs { get; init; } = [];

    /// <summary>输出字段定义</summary>
    public IReadOnlyList<FieldDef> Outputs { get; init; } = [];

    /// <summary>所有字段（输入 + 输出）</summary>
    public IEnumerable<FieldDef> AllFields => Inputs.Concat(Outputs);
}