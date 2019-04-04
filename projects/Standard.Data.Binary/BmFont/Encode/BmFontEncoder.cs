using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.BmFont.Data;

namespace Std.Data.Binary.BmFont.Encode;

/// <summary>
///     BMFont 编码器，的<see cref="BmFontData" /> 编码的BMFont 二进制格式的
/// </summary>
/// <remarks>
///     BMFont 的AngelCode 的位图字体格式，广泛用于游戏 UI 渲染的
///     编码器输的BMFont 二进制格式（.fnt），文本格式的Oak.BmFont 处理的
/// </remarks>
public sealed class BmFontEncoder
{
    /// <summary>
    ///     的BMFont 数据编码为二进制字节数组的
    /// </summary>
    /// <param name="data">
    ///     BMFont 数据的/param>
    ///     <returns>BMFont 二进制数据的/returns>
    public byte[] encode(BmFontData data)
    {
        var blocks = new List<byte[]>();

        // 编码 `Info` 块。
        blocks.Add(encode_info_block(data.info));

        // 编码 `Common` 块。
        blocks.Add(encode_common_block(data.common));

        // 编码 `Pages` 块。
        if (data.pages.Count > 0) blocks.Add(encode_pages_block(data.pages, data.common.pages));

        // 编码 `Chars` 块。
        if (data.chars.Count > 0) blocks.Add(encode_chars_block(data.chars));

        // 编码 `Kerning` 块。
        if (data.kerning_pairs.Count > 0) blocks.Add(encode_kerning_block(data.kerning_pairs));

        // 计算总大小并组装
        var totalSize = 4; // BMF + version

        foreach (var block in blocks) totalSize += block.Length;

        var buffer = new byte[totalSize];
        var pos = 0;

        buffer[pos++] = (byte)'B';
        buffer[pos++] = (byte)'M';
        buffer[pos++] = (byte)'F';
        buffer[pos++] = BmFontConstants.binary_version;

        foreach (var block in blocks)
        {
            block.CopyTo(buffer.AsSpan(pos));
            pos += block.Length;
        }

        return buffer;
    }

    #region 块编的

    private static byte[] encode_info_block(BmFontInfo info)
    {
        var nameBytes = Encoding.UTF8.GetBytes(info.font_name);
        var dataSize = 13 + nameBytes.Length + 1; // 13 字节固定的+ 名称 + null
        var block = new byte[5 + dataSize]; // 类型(1) + 大小(4) + 数据
        var pos = 0;

        block[pos++] = BmFontConstants.block_info;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.size);
        pos += 2;

        byte flags = 0;

        if (info.bold) flags |= 0x01;

        if (info.italic) flags |= 0x02;

        if (info.unicode) flags |= 0x04;

        block[pos++] = flags;
        block[pos++] = info.bit_depth;
        block[pos++] = info.char_set;
        pos += 2; // stretchH 填充

        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.spacing_h);
        pos += 2;
        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.spacing_v);
        pos += 2;
        BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), info.line_height);
        pos += 2;

        nameBytes.CopyTo(block.AsSpan(pos));
        pos += nameBytes.Length;
        block[pos] = 0; // null 结尾

        return block;
    }

    private static byte[] encode_common_block(BmFontCommon common)
    {
        const int dataSize = 15;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.block_common;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), dataSize);
        pos += 4;

        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.line_height);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.@base);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.scale_w);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.scale_h);
        pos += 2;
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), common.pages);
        pos += 2;

        byte flags = 0;

        if (common.alpha_channel) flags |= 0x01;

        if (common.red_channel) flags |= 0x02;

        if (common.green_channel) flags |= 0x04;

        if (common.blue_channel) flags |= 0x08;

        if (common.packed) flags |= 0x10;

        block[pos++] = flags;
        pos += 3; // 保留填充

        return block;
    }

    private static byte[] encode_pages_block(IReadOnlyList<string> pages, ushort pageCount)
    {
        if (pages.Count == 0) return [];

        // 所有页面名称等长填充
        var maxLen = 0;

        foreach (var page in pages)
        {
            var len = Encoding.UTF8.GetByteCount(page);

            if (len > maxLen) maxLen = len;
        }

        var paddedLen = maxLen + 1;
        var dataSize = pageCount * paddedLen;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.block_pages;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        for (var i = 0; i < pageCount && i < pages.Count; i++)
        {
            var nameBytes = Encoding.UTF8.GetBytes(pages[i]);
            nameBytes.CopyTo(block.AsSpan(pos));
            pos += paddedLen; // 直接跳到下一个页面名称位置
        }

        return block;
    }

    private static byte[] encode_chars_block(IReadOnlyList<BmFontChar> chars)
    {
        const int charSize = 20;
        var dataSize = chars.Count * charSize;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.block_chars;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        foreach (var c in chars)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), c.id);
            pos += 4;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.x);
            pos += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.y);
            pos += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.width);
            pos += 2;
            BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(pos), c.height);
            pos += 2;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), c.x_offset);
            pos += 2;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), c.y_offset);
            pos += 2;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), c.x_advance);
            pos += 2;
            block[pos++] = c.page;
            block[pos++] = (byte)c.channel;
        }

        return block;
    }

    private static byte[] encode_kerning_block(IReadOnlyList<BmFontKerningPair> pairs)
    {
        const int pairSize = 10;
        var dataSize = pairs.Count * pairSize;
        var block = new byte[5 + dataSize];
        var pos = 0;

        block[pos++] = BmFontConstants.block_kerning_pairs;
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), (uint)dataSize);
        pos += 4;

        foreach (var p in pairs)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), p.first);
            pos += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(pos), p.second);
            pos += 4;
            BinaryPrimitives.WriteInt16LittleEndian(block.AsSpan(pos), p.amount);
            pos += 2;
        }

        return block;
    }

    #endregion
}