using Std.Data.Binary.Frame;
using Std.Data.Binary.Hashing;
using Std.Data.Binary.Ogg.Data;

namespace Std.Data.Binary.Ogg.Encode;

/// <summary>
///     OGG 文件编码器，的C# 数据结构编码的OGG 容器格式的
/// </summary>
/// <remarks>
///     OGG 的Xiph.Org 的开源容器格式，常用于封的Vorbis 的Opus 音频的
///     编码器将数据包序列封装为 OGG 页面，生成符的OGG 规范的二进制数据的
/// </remarks>
public sealed class OggEncoder
{
    /// <summary>
    ///     的OGG 音频数据编码的OGG 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     OGG 音频数据的/param>
    ///     <returns>OGG 二进制数据的/returns>
    public byte[] encode(OggAudioData data)
    {
        var pages = build_pages(data);
        var size = estimate_size(pages);
        var buffer = new byte[size];
        var writer = new ByteBufferWriter(buffer);

        foreach (var page in pages) write_page(ref writer, page);

        return buffer[..writer.position];
    }

    #region 私有编码方法

    private static List<OggEncodePage> build_pages(OggAudioData data)
    {
        var pages = new List<OggEncodePage>();
        var packetIndex = 0;
        var pageSequence = 0u;
        var serialNumber = 0u;

        while (packetIndex < data.packets.Count)
        {
            var page = build_page(data.packets, ref packetIndex, pageSequence, serialNumber, pages.Count == 0);
            pages.Add(page);
            pageSequence++;
        }

        if (pages.Count > 0) pages[pages.Count - 1].flags |= OggPageFlags.end_of_stream;

        return pages;
    }

    private static OggEncodePage build_page(IReadOnlyList<byte[]> packets, ref int packetIndex, uint pageSequence,
        uint serialNumber, bool isFirstPage)
    {
        var segments = new List<byte[]>();
        var segmentSizes = new List<int>();
        var flags = OggPageFlags.none;

        if (isFirstPage) flags |= OggPageFlags.begin_of_stream;

        var remainingSegments = 255;
        var packetFinished = false;

        while (packetIndex < packets.Count && remainingSegments > 0 && !packetFinished)
        {
            var packet = packets[packetIndex];
            var offset = 0;

            while (offset < packet.Length && remainingSegments > 0)
            {
                var chunkSize = System.Math.Min(packet.Length - offset, 255);
                segments.Add(packet[offset..(offset + chunkSize)]);
                segmentSizes.Add(chunkSize);
                offset += chunkSize;
                remainingSegments--;
            }

            if (offset >= packet.Length)
            {
                packetIndex++;
                packetFinished = true;
            }
        }

        var pageData = new List<byte>();

        foreach (var seg in segments) pageData.AddRange(seg);

        var page = new OggEncodePage
        {
            granule_position = 0,
            serial_number = serialNumber,
            page_sequence_number = pageSequence,
            segment_sizes = segmentSizes,
            data = [.. pageData],
            flags = flags
        };
        return page;
    }

    private static void write_page(ref ByteBufferWriter writer, OggEncodePage page)
    {
        var headerSize = OggConstants.page_header_size + page.segment_sizes.Count;
        var pageSize = headerSize + page.data.Length;
        var headerBuffer = new byte[pageSize];
        var headerWriter = new ByteBufferWriter(headerBuffer);

        headerWriter.write_string("OggS");
        headerWriter.write_u8(OggConstants.version);
        headerWriter.write_u8((byte)page.flags);
        write_u64_le(ref headerWriter, page.granule_position);
        headerWriter.write_u32_le(page.serial_number);
        headerWriter.write_u32_le(page.page_sequence_number);
        headerWriter.write_u32_le(0);
        headerWriter.write_u8((byte)page.segment_sizes.Count);

        foreach (var size in page.segment_sizes) headerWriter.write_u8((byte)size);

        headerWriter.write(page.data);

        var crc = compute_ogg_crc(headerBuffer[..headerWriter.position]);
        var crcBytes = headerBuffer.AsSpan(22, 4);
        crcBytes[0] = (byte)(crc & 0xFF);
        crcBytes[1] = (byte)((crc >> 8) & 0xFF);
        crcBytes[2] = (byte)((crc >> 16) & 0xFF);
        crcBytes[3] = (byte)((crc >> 24) & 0xFF);

        writer.write(headerBuffer[..headerWriter.position]);
    }

    private static void write_u64_le(ref ByteBufferWriter writer, ulong value)
    {
        writer.write_u32_le((uint)(value & 0xFFFFFFFF));
        writer.write_u32_le((uint)(value >> 32));
    }

    private static int estimate_size(List<OggEncodePage> pages)
    {
        var total = 0;

        foreach (var page in pages)
            total += OggConstants.page_header_size + page.segment_sizes.Count + page.data.Length;

        return total + 256;
    }

    private static uint compute_ogg_crc(ReadOnlySpan<byte> data)
    {
        var crc = new Crc32(Crc32.normal_polynomial, 0, 0, false);

        crc.update(data);

        return crc.value;
    }

    #endregion
}

/// <summary>
///     OGG 编码页面（内部使用）的
/// </summary>
internal sealed class OggEncodePage
{
    public OggPageFlags flags { get; set; }
    public ulong granule_position { get; init; }
    public uint serial_number { get; init; }
    public uint page_sequence_number { get; init; }
    public List<int> segment_sizes { get; init; } = [];
    public byte[] data { get; init; } = [];
}