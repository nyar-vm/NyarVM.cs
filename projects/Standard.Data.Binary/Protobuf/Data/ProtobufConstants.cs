namespace Std.Data.Binary.Protobuf.Data;

/// <summary>
///     Protobuf 相关常量定义的
/// </summary>
public static class ProtobufConstants
{
    /// <summary>
    ///     Protobuf wire type 定义的
    /// </summary>
    public enum WireType
    {
        /// <summary>
        ///     可变长度整数的
        /// </summary>
        varint = 0,


        /// <summary>
        ///     64 位值的
        /// </summary>
        fixed64 = 1,


        /// <summary>
        ///     长度前缀的字符串/消息的
        /// </summary>
        length_delimited = 2,


        /// <summary>
        ///     32 位值的
        /// </summary>
        fixed32 = 5
    }

    /// <summary>
    ///     最大字段号的
    /// </summary>
    public const int max_field_number = 19000;

    /// <summary>
    ///     字段号掩码的
    /// </summary>
    public const int field_number_mask = 0x07;

    /// <summary>
    ///     Wire type 掩码的
    /// </summary>
    public const int wire_type_mask = 0x7F;
}