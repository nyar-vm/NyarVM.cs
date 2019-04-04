namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     表流的~ 的#-）的
/// </summary>
public sealed class ClrTableStream
{
    /// <summary>
    ///     表头部的
    /// </summary>
    public ClrTableHeader header { get; init; } = new();

    /// <summary>
    ///     各表行数据，的<see cref="ClrTableKind" /> 索引的
    /// </summary>
    public IReadOnlyList<ClrTableData> tables { get; init; } = [];
}