using Std.Data.Binary.Frame;
using Std.Data.Binary.Protobuf.Data;

namespace Std.Data.Binary.Protobuf.Decode;

/// <summary>
///     Protobuf 解码器，用于解析 Protobuf 二进制消息的
/// </summary>
public static class ProtobufDecoder
{
    /// <summary>
    ///     解码 Protobuf 消息的
    /// </summary>
    /// <param name="data">
    ///     要解码的二进制数据的/param>
    ///     <returns>解码后的 Protobuf 消息数据的/returns>
    public static ProtobufMessageData decode_message(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);
        var message = new ProtobufMessageData
        {
            name = "RootMessage"
        };

        while (!buffer.is_end)
        {
            var field = decode_field(buffer);
            message.fields.Add(field);
        }

        return message;
    }

    /// <summary>
    ///     解码字段的
    /// </summary>
    /// <param name="buffer">
    ///     字节缓冲区的/param>
    ///     <returns>解码后的字段数据的/returns>
    private static ProtobufFieldData decode_field(ByteBuffer buffer)
    {
        var tag = buffer.read_leb128_u32();
        var fieldNumber = (int)(tag >> 3);
        var wireType = (ProtobufConstants.WireType)(tag & 0x07);

        var field = new ProtobufFieldData
        {
            field_number = fieldNumber,
            type = string.Empty,
            name = string.Empty
        };

        switch (wireType)
        {
            case ProtobufConstants.WireType.varint:
                decode_varint_field(buffer, field);
                break;
            case ProtobufConstants.WireType.fixed64:
                decode_fixed64_field(buffer, field);
                break;
            case ProtobufConstants.WireType.length_delimited:
                decode_length_delimited_field(buffer, field);
                break;
            case ProtobufConstants.WireType.fixed32:
                decode_fixed32_field(buffer, field);
                break;
        }

        return field;
    }

    /// <summary>
    ///     解码 Varint 字段的
    /// </summary>
    /// <param name="buffer">
    ///     字节缓冲区的/param>
    ///     <param name="field">字段数据的/param>
    private static void decode_varint_field(ByteBuffer buffer, ProtobufFieldData field)
    {
        _ = buffer.read_leb128_u64();
        field.type = "varint";
        field.name = $"field_{field.field_number}";
    }

    /// <summary>
    ///     解码 Fixed64 字段的
    /// </summary>
    /// <param name="buffer">
    ///     字节缓冲区的/param>
    ///     <param name="field">字段数据的/param>
    private static void decode_fixed64_field(ByteBuffer buffer, ProtobufFieldData field)
    {
        _ = buffer.read_u64_le();
        field.type = "fixed64";
        field.name = $"field_{field.field_number}";
    }

    /// <summary>
    ///     解码 LengthDelimited 字段的
    /// </summary>
    /// <param name="buffer">
    ///     字节缓冲区的/param>
    ///     <param name="field">字段数据的/param>
    private static void decode_length_delimited_field(ByteBuffer buffer, ProtobufFieldData field)
    {
        var length = buffer.read_leb128_u32();
        buffer.advance((int)length);
        field.type = "length_delimited";
        field.name = $"field_{field.field_number}";
    }

    /// <summary>
    ///     解码 Fixed32 字段的
    /// </summary>
    /// <param name="buffer">
    ///     字节缓冲区的/param>
    ///     <param name="field">字段数据的/param>
    private static void decode_fixed32_field(ByteBuffer buffer, ProtobufFieldData field)
    {
        _ = buffer.read_u32_le();
        field.type = "fixed32";
        field.name = $"field_{field.field_number}";
    }
}