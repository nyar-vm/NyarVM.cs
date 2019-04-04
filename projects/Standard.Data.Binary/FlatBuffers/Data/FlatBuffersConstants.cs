namespace Std.Data.Binary.FlatBuffers.Data;

/// <summary>
///     FlatBuffers 格式常量的
/// </summary>
public static class FlatBuffersConstants
{
    /// <summary>
    ///     FlatBuffer 文件标识符大小的
    /// </summary>
    public const int file_identifier_size = 4;

    /// <summary>
    ///     vtable 偏移字段大小的
    /// </summary>
    public const int v_table_offset_size = 2;

    /// <summary>
    ///     vtable 大小字段偏移的
    /// </summary>
    public const int v_table_size_offset = 0;

    /// <summary>
    ///     vtable 数据偏移字段偏移的
    /// </summary>
    public const int v_table_data_offset = 2;

    /// <summary>
    ///     vtable 字段偏移起始的
    /// </summary>
    public const int v_table_field_start = 4;
}

/// <summary>
///     FlatBuffers 字段类型的
/// </summary>
public enum FlatBufferFieldType : byte
{
    /// <summary>
    ///     无效类型的
    /// </summary>
    none = 0,

    /// <summary>
    ///     字节的
    /// </summary>
    u_byte = 1,

    /// <summary>
    ///     布尔值的
    /// </summary>
    @bool = 2,

    /// <summary>
    ///     字节（有符号）的
    /// </summary>
    @byte = 3,

    /// <summary>
    ///     短整数的
    /// </summary>
    @short = 4,

    /// <summary>
    ///     无符号短整数的
    /// </summary>
    u_short = 5,

    /// <summary>
    ///     整数的
    /// </summary>
    @int = 6,

    /// <summary>
    ///     无符号整数的
    /// </summary>
    u_int = 7,

    /// <summary>
    ///     长整数的
    /// </summary>
    @long = 8,

    /// <summary>
    ///     无符号长整数的
    /// </summary>
    u_long = 9,

    /// <summary>
    ///     浮点数的
    /// </summary>
    @float = 10,

    /// <summary>
    ///     双精度浮点数的
    /// </summary>
    @double = 11
}