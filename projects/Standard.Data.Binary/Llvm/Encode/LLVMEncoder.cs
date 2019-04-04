using System.Buffers.Binary;
using Std.Data.Binary.Llvm.Data;

namespace Std.Data.Binary.Llvm.Encode;

/// <summary>
///     LLVM 位码编码器，的LLVM 位码数据结构编码的LLVM Bitcode 二进制格式的
/// </summary>
/// <remarks>
///     本编码器生成 LLVM 3.7 兼容的Bitcode 格式，用的DXIL 着色器程序的
///     LLVM Bitcode 使用基于位流的变长编码，包含块和记录两种结构的
///     编码器遵的Acorn 架构规则：二进制编解码职责由 Acorn 独占的
/// </remarks>
public sealed class LlvmEncoder
{
    /// <summary>
    ///     LLVM Bitcode 魔数的BC" + 0xC0 + 0xDE）的
    /// </summary>
    public const uint bitcode_magic = 0xDEC04242u;


    /// <summary>
    ///     的LLVM 位码数据编码的Bitcode 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     LLVM 位码数据的/param>
    ///     <returns>LLVM Bitcode 二进制数据的/returns>
    public byte[] encode(LlvmBitcodeData data)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(new byte[] { 0x42, 0x43, 0xC0, 0xDE });

        write_u16_le(writer, data.magic.version);

        write_blocks(writer, data.top_level_blocks);

        writer.Flush();
        return stream.ToArray();
    }


    /// <summary>
    ///     将块列表编码的Bitcode 二进制格式的
    /// </summary>
    /// <param name="blocks">
    ///     块列表的/param>
    ///     <returns>Bitcode 二进制数据（不含魔数）的/returns>
    public byte[] encode_blocks(IReadOnlyList<LlvmBlockData> blocks)
    {
        using var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        write_blocks(writer, blocks);

        writer.Flush();
        return stream.ToArray();
    }

    private static void write_blocks(BinaryWriter writer, IReadOnlyList<LlvmBlockData> blocks)
    {
        foreach (var block in blocks) write_block(writer, block);
    }

    private static void write_block(BinaryWriter writer, LlvmBlockData block)
    {
        write_leb128_u32(writer, block.block_id);

        using var blockStream = new MemoryStream();
        var blockWriter = new BinaryWriter(blockStream);

        foreach (var subBlock in block.sub_blocks)
        {
            write_leb128_u32(blockWriter, 1u);
            write_block(blockWriter, subBlock);
        }

        foreach (var record in block.records) write_record(blockWriter, record);

        write_leb128_u32(blockWriter, 2u);

        blockWriter.Flush();
        var blockData = blockStream.ToArray();

        write_leb128_u32(writer, (uint)blockData.Length);
        writer.Write(blockData);
    }

    private static void write_record(BinaryWriter writer, LlvmRecordData record)
    {
        write_leb128_u32(writer, record.code);
        write_leb128_u32(writer, (uint)record.operands.Count);

        foreach (var operand in record.operands) write_leb128_u64(writer, operand);
    }

    private static void write_u16_le(BinaryWriter writer, ushort value)
    {
        var bytes = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void write_leb128_u32(BinaryWriter writer, uint value)
    {
        do
        {
            var byteVal = value & 0x7Fu;
            value >>= 7;

            if (value != 0) byteVal |= 0x80u;

            writer.Write((byte)byteVal);
        } while (value != 0);
    }

    private static void write_leb128_u64(BinaryWriter writer, ulong value)
    {
        do
        {
            var byteVal = value & 0x7FUL;
            value >>= 7;

            if (value != 0) byteVal |= 0x80u;

            writer.Write((byte)byteVal);
        } while (value != 0);
    }
}