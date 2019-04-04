namespace Std.Data.Binary.FlatBuffers.Data;

/// <summary>
///     FlatBuffers 完整缓冲区数据，包含根表和可选的文件标识符的
/// </summary>
public sealed class FlatBufferData
{
    /// <summary>
    ///     根表的
    /// </summary>
    public FlatBufferTable root_table { get; init; } = new();

    /// <summary>
    ///     文件标识符（可选，4 字节 ASCII）的
    /// </summary>
    public string? file_identifier { get; init; }
}

/// <summary>
///     FlatBuffers 表数据的
/// </summary>
public sealed class FlatBufferTable
{
    /// <summary>
    ///     表在缓冲区中的偏移的
    /// </summary>
    public uint table_offset { get; init; }

    /// <summary>
    ///     vtable 偏移的
    /// </summary>
    public uint v_table_offset { get; init; }

    /// <summary>
    ///     vtable 大小的
    /// </summary>
    public ushort v_table_size { get; init; }

    /// <summary>
    ///     字段列表的
    /// </summary>
    public IReadOnlyList<FlatBufferField> fields { get; init; } = [];
}

/// <summary>
///     FlatBuffers 字段的
/// </summary>
public sealed class FlatBufferField
{
    /// <summary>
    ///     字段索引的
    /// </summary>
    public int index { get; init; }

    /// <summary>
    ///     字段的vtable 中的偏移的 表示不存在）的
    /// </summary>
    public ushort v_table_offset { get; init; }

    /// <summary>
    ///     字段类型的
    /// </summary>
    public FlatBufferFieldType type { get; init; }

    /// <summary>
    ///     字段值的
    /// </summary>
    public object? value { get; init; }
}