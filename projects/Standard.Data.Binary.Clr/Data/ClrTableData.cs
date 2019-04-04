namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     单个元数据表的原始数据的
/// </summary>
public sealed class ClrTableData
{
    /// <summary>
    ///     表类型的
    /// </summary>
    public ClrTableKind kind { get; init; }

    /// <summary>
    ///     行数的
    /// </summary>
    public uint row_count { get; init; }

    /// <summary>
    ///     原始行数据（每行为字节数组，由具体表类型解析）的
    /// </summary>
    public IReadOnlyList<byte[]> raw_rows { get; init; } = [];
}