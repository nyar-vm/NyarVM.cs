using Std.Data.Binary.Frame;
using Std.Data.Binary.Ogg.Data;

namespace Std.Data.Binary.Ogg.Decode;

/// <summary>
///     OGG 文件解码器，的OGG 容器格式解码的C# 数据结构的
/// </summary>
/// <remarks>
///     OGG 的Xiph.Org 的开源容器格式，常用于封的Vorbis 的Opus 音频的
///     解码器解的OGG 页面结构并提取音频数据包，不执行音频解码的
/// </remarks>
public ref struct OggDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="OggDecoder" /> 结构的新实例的
    /// </summary>
    /// <param name="data">OGG 二进制数据的/param>
    public OggDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     获取当前在流中的位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 OGG 文件，提取容器级信息的
    /// </summary>
    /// <returns>OGG 音频数据的/returns>
    public OggAudioData decode()
    {
        var packets = new List<byte[]>();
        var pageCount = 0;
        var codecType = OggCodecType.unknown;
        var channels = 0;
        var sampleRate = 0;
        var nominalBitrate = 0;

        while (!_buffer.is_end)
        {
            var page = read_page();

            if (page == null) break;

            pageCount++;

            foreach (var packet in page.Value.packets) packets.Add(packet);

            if (pageCount == 1 && packets.Count > 0)
                (codecType, channels, sampleRate, nominalBitrate) = parse_identification_header(packets[0]);
        }

        return new OggAudioData
        {
            codec_type = codecType,
            channels = channels,
            sample_rate = sampleRate,
            nominal_bitrate = nominalBitrate,
            page_count = pageCount,
            packets = packets
        };
    }

    #region 私有解析方法

    private OggPage? read_page()
    {
        if (_buffer.remaining < OggConstants.page_header_size) return null;

        var capture = _buffer.read_string(4);

        if (capture != "OggS") return null;

        var version = _buffer.read_u8();

        if (version != OggConstants.version) throw new InvalidDataException($"OGG 版本号无效，期望 0，实的{version}");

        var flags = (OggPageFlags)_buffer.read_u8();
        var granulePosition = read_u64_le();
        var serialNumber = _buffer.read_u32_le();
        var pageSequenceNumber = _buffer.read_u32_le();
        var checksum = _buffer.read_u32_le();
        var segmentCount = _buffer.read_u8();

        if (_buffer.remaining < segmentCount) return null;

        var segmentSizes = new int[segmentCount];
        var totalDataSize = 0;

        for (var i = 0; i < segmentCount; i++)
        {
            segmentSizes[i] = _buffer.read_u8();
            totalDataSize += segmentSizes[i];
        }

        if (_buffer.remaining < totalDataSize) return null;

        var pageData = _buffer.read_bytes(totalDataSize).ToArray();

        var packets = reassemble_packets(pageData, segmentSizes, flags);

        var page = new OggPage
        {
            flags = flags,
            granule_position = granulePosition,
            serial_number = serialNumber,
            page_sequence_number = pageSequenceNumber,
            packets = packets
        };
        return page;
    }

    private static List<byte[]> reassemble_packets(byte[] pageData, int[] segmentSizes, OggPageFlags flags)
    {
        var packets = new List<byte[]>();
        var offset = 0;
        var currentPacket = new List<byte>();
        var isContinued = (flags & OggPageFlags.continued) != 0;

        for (var i = 0; i < segmentSizes.Length; i++)
        {
            var size = segmentSizes[i];

            if (offset + size > pageData.Length) break;

            if (size > 0) currentPacket.AddRange(pageData[offset..(offset + size)]);

            offset += size;

            if (size < 255)
            {
                if (!isContinued || currentPacket.Count > 0) packets.Add([.. currentPacket]);

                currentPacket.Clear();
                isContinued = false;
            }
        }

        if (currentPacket.Count > 0) packets.Add([.. currentPacket]);

        return packets;
    }

    private static (OggCodecType codecType, int channels, int sampleRate, int nominalBitrate)
        parse_identification_header(byte[] header)
    {
        if (header.Length < 7) return (OggCodecType.unknown, 0, 0, 0);

        if (header[0] == 0x01 && header.AsSpan(1, 6).SequenceEqual(OggConstants.vorbis_id))
        {
            if (header.Length < 30) return (OggCodecType.vorbis, 0, 0, 0);

            var channels = header[11];
            var sampleRate = (int)read_le32(header, 12);
            var nominalBitrate = (int)read_le32(header, 16);

            return (OggCodecType.vorbis, channels, sampleRate, nominalBitrate);
        }

        if (header.AsSpan(0, 8).SequenceEqual(OggConstants.opus_id))
        {
            if (header.Length < 19) return (OggCodecType.opus, 0, 0, 0);

            var channels = header[9];
            var sampleRate = (int)read_le32(header, 12);

            return (OggCodecType.opus, channels, sampleRate, 0);
        }

        return (OggCodecType.unknown, 0, 0, 0);
    }

    private static uint read_le32(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    private ulong read_u64_le()
    {
        var low = _buffer.read_u32_le();
        var high = _buffer.read_u32_le();
        return low | ((ulong)high << 32);
    }

    #endregion
}

/// <summary>
///     OGG 页面数据（内部使用）的
/// </summary>
internal struct OggPage
{
    public OggPageFlags flags;
    public ulong granule_position;
    public uint serial_number;
    public uint page_sequence_number;
    public List<byte[]> packets;
}