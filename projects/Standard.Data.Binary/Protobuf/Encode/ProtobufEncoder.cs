using Std.Data.Binary.Frame;
using Std.Data.Binary.Protobuf.Data;

namespace Std.Data.Binary.Protobuf.Encode;

/// <summary>
///     Protobuf 编码器，用于生成 Protobuf 二进制消息的
/// </summary>
public static class ProtobufEncoder
{
    /// <summary>
    ///     编码 Protobuf 消息的
    /// </summary>
    /// <param name="buffer">
    ///     要写入的目标字节缓冲区的/param>
    ///     <param name="message">Protobuf 消息数据的/param>
    public static void encode_message(Span<byte> buffer, ProtobufMessageData message)
    {
        var writer = new ByteBufferWriter(buffer);
        encode_message_core(writer, message);
    }

    /// <summary>
    ///     编码 Protobuf 消息到字节数组的
    /// </summary>
    /// <param name="message">
    ///     Protobuf 消息数据的/param>
    ///     <returns>编码后的字节数组的/returns>
    public static byte[] encode_message(ProtobufMessageData message)
    {
        var temp = new byte[4096];
        var writer = new ByteBufferWriter(temp);
        encode_message_core(writer, message);
        return [.. temp[..writer.position]];
    }

    /// <summary>
    ///     编码 Protobuf 消息核心逻辑的
    /// </summary>
    /// <param name="writer">
    ///     字节缓冲区写入器的/param>
    ///     <param name="message">Protobuf 消息数据的/param>
    private static void encode_message_core(ByteBufferWriter writer, ProtobufMessageData message)
    {
        foreach (var field in message.fields) encode_field(writer, field);

        foreach (var nestedMessage in message.nested_messages) encode_message_core(writer, nestedMessage);
    }

    /// <summary>
    ///     编码字段的
    /// </summary>
    /// <param name="writer">
    ///     字节缓冲区写入器的/param>
    ///     <param name="field">字段数据的/param>
    private static void encode_field(ByteBufferWriter writer, ProtobufFieldData field)
    {
        var tag = (uint)(field.field_number << 3);

        switch (field.type)
        {
            case "varint":
                tag |= (uint)ProtobufConstants.WireType.varint;
                writer.write_leb128_u32(tag);
                writer.write_leb128_u64(0);
                break;
            case "fixed64":
                tag |= (uint)ProtobufConstants.WireType.fixed64;
                writer.write_leb128_u32(tag);
                writer.write_u64_le(0);
                break;
            case "length_delimited":
                tag |= (uint)ProtobufConstants.WireType.length_delimited;
                writer.write_leb128_u32(tag);
                writer.write_leb128_u32(0);
                break;
            case "fixed32":
                tag |= (uint)ProtobufConstants.WireType.fixed32;
                writer.write_leb128_u32(tag);
                writer.write_u32_le(0);
                break;
        }
    }
}