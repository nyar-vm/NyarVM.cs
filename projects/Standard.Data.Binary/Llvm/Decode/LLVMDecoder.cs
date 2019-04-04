using Std.Data.Binary.Frame;
using Std.Data.Binary.Llvm.Data;

namespace Std.Data.Binary.Llvm.Decode;

/// <summary>
///     LLVM 位码文件解码器，解析 LLVM 位码的bc）格式的
/// </summary>
public sealed class LlvmDecoder
{
    /// <summary>
    ///     的LLVM 位码二进制数据解码文件的
    /// </summary>
    /// <param name="data">
    ///     LLVM 位码二进制数据的/param>
    ///     <returns>解码后的 LLVM 位码文件数据的/returns>
    public LlvmBitcodeData decode(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        return decode_file(ref buffer);
    }

    private LlvmBitcodeData decode_file(ref ByteBuffer buffer)
    {
        var magicBytes = buffer.read_bytes(4).ToArray();

        if (magicBytes[0] != 0x42 || magicBytes[1] != 0x43 || magicBytes[2] != 0xC0 || magicBytes[3] != 0xDE)
            throw new InvalidDataException("不是有效的 LLVM 位码文件（魔数不匹配）。");

        var magic = read_magic(ref buffer);
        var blocks = read_blocks(ref buffer);

        return new LlvmBitcodeData
        {
            magic = magic,
            top_level_blocks = blocks
        };
    }


    /// <summary>
    ///     读取魔数信息的
    /// </summary>
    private LlvmMagicData read_magic(ref ByteBuffer buffer)
    {
        var version = buffer.read_u16_le();

        return new LlvmMagicData
        {
            magic = 0x42C0DE00u | version,
            version = version
        };
    }


    /// <summary>
    ///     读取块列表的
    /// </summary>
    private List<LlvmBlockData> read_blocks(ref ByteBuffer buffer)
    {
        var blocks = new List<LlvmBlockData>();

        while (!buffer.is_end)
        {
            var block = read_block(ref buffer);
            if (block != null)
                blocks.Add(block);
            else
                break;
        }

        return blocks;
    }


    /// <summary>
    ///     读取单个块的
    /// </summary>
    private LlvmBlockData? read_block(ref ByteBuffer buffer)
    {
        if (buffer.remaining < 8) return null;

        var blockId = buffer.read_leb128_u32();
        var blockSize = buffer.read_leb128_u32();

        if (blockSize == 0 || buffer.position + blockSize > buffer.length) return null;

        var blockEnd = buffer.position + (int)blockSize;
        var records = new List<LlvmRecordData>();
        var subBlocks = new List<LlvmBlockData>();

        while (buffer.position < blockEnd)
        {
            var code = buffer.read_leb128_u32();

            if (code == 0) break;

            if (code == 1)
            {
                var subBlock = read_block(ref buffer);
                if (subBlock != null) subBlocks.Add(subBlock);

                continue;
            }

            if (code == 2)
            {
                buffer.position = System.Math.Min(blockEnd, buffer.length);
                break;
            }

            var record = read_record(ref buffer, code);
            records.Add(record);
        }

        buffer.position = System.Math.Min(blockEnd, buffer.length);

        return new LlvmBlockData
        {
            block_id = blockId,
            block_size = blockSize,
            records = records,
            sub_blocks = subBlocks
        };
    }


    /// <summary>
    ///     读取记录的
    /// </summary>
    private LlvmRecordData read_record(ref ByteBuffer buffer, uint code)
    {
        var operandCount = buffer.read_leb128_u32();
        var operands = new List<ulong>();

        for (var i = 0; i < operandCount; i++) operands.Add(buffer.read_leb128_u64());

        return new LlvmRecordData
        {
            code = code,
            operands = operands
        };
    }
}